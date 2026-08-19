import argparse
import http.server
import json
import os
import pathlib
import subprocess
import threading
import time


ROOT = pathlib.Path(r"D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main")
WWW_ROOT = ROOT / "camera_www"
EDGE = pathlib.Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")
HOST = "127.0.0.1"
CAMERAS = {
    "cam1": {"key": "cam1", "display_name": "USB Camera"},
    "cam2": {"key": "cam2", "display_name": "A4Tech Camera"},
}


def build_capture_page(camera):
    configs = json.dumps((camera,))
    return f"""<!doctype html>
<meta charset=\"utf-8\">
<script>
const cameras = {configs};
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const active = new Map();

async function postStatus(key, state, detail = {{}}) {{
  try {{
    await fetch(`/status/${{key}}`, {{
      method: 'POST',
      headers: {{'Content-Type': 'application/json'}},
      body: JSON.stringify({{state, cameraKey: key, ...detail, at: new Date().toISOString()}})
    }});
  }} catch (_) {{}}
}}

function isRequestedCamera(key, label) {{
  const normalized = (label || '').trim().toLowerCase();
  if (key === 'cam1') {{
    return (normalized === 'usb camera' || normalized.includes('usb camera') ||
      normalized.includes('4c4a:4a55')) &&
      !normalized.includes('a4 tech') && !normalized.includes('0ac8:3450');
  }}
  return normalized.includes('a4 tech') || normalized.includes('0ac8:3450');
}}

async function openCamera(config, devices) {{
  if (active.has(config.key)) return;

  const device = devices.find(item => isRequestedCamera(config.key, item.label));
  if (!device) {{
    await postStatus(config.key, 'error', {{error: `${{config.display_name}} was not found`}});
    return;
  }}

  try {{
    const stream = await navigator.mediaDevices.getUserMedia({{
      video: {{
        deviceId: {{exact: device.deviceId}},
        width: {{ideal: 640}},
        height: {{ideal: 480}},
        frameRate: {{ideal: 15}}
      }},
      audio: false
    }});
    await activateCamera(config, stream, device.label || config.display_name);
  }} catch (error) {{
    await postStatus(config.key, 'error', {{
      error: `${{error.name || 'Error'}}: ${{error.message || error}}`
    }});
  }}
}}

async function activateCamera(config, stream, cameraName) {{
    const video = document.createElement('video');
    video.autoplay = true;
    video.muted = true;
    video.playsInline = true;
    video.srcObject = stream;
    await video.play();
    while (!video.videoWidth || !video.videoHeight) {{ await sleep(100); }}

    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    active.set(config.key, {{video, canvas, context: canvas.getContext('2d', {{alpha: false}})}});
    await postStatus(config.key, 'online', {{
      camera: cameraName,
      width: video.videoWidth,
      height: video.videoHeight
    }});
}}

async function sendFrame(key, capture) {{
  const track = capture.video.srcObject?.getVideoTracks?.()[0];
  if (!track || track.readyState !== 'live') {{
    throw new Error('Camera stream ended');
  }}
  capture.context.drawImage(capture.video, 0, 0, capture.canvas.width, capture.canvas.height);
  const blob = await new Promise(resolve => capture.canvas.toBlob(resolve, 'image/jpeg', 0.78));
  if (blob && blob.size > 1000) {{
    await fetch(`/frame/${{key}}`, {{method: 'POST', body: blob}});
  }}
}}

(async function run() {{
  try {{
    // One short permission request exposes labels for both cameras in this Edge instance.
    const permissionStream = await navigator.mediaDevices.getUserMedia({{video: true, audio: false}});
    const permissionTrack = permissionStream.getVideoTracks()[0];
    const config = cameras[0];
    if (permissionTrack && isRequestedCamera(config.key, permissionTrack.label)) {{
      await activateCamera(config, permissionStream, permissionTrack.label || config.display_name);
    }} else {{
      permissionStream.getTracks().forEach(track => track.stop());
      await sleep(1500);
    }}

    while (true) {{
      const devices = (await navigator.mediaDevices.enumerateDevices())
        .filter(device => device.kind === 'videoinput');
      for (const config of cameras) {{
        await openCamera(config, devices);
      }}
      for (const [key, capture] of active) {{
        try {{
          await sendFrame(key, capture);
        }} catch (error) {{
          active.delete(key);
          await postStatus(key, 'error', {{error: `${{error.name || 'Error'}}: ${{error.message || error}}`}});
        }}
      }}
      await sleep(500);
    }}
  }} catch (error) {{
    for (const config of cameras) {{
      await postStatus(config.key, 'error', {{
        error: `${{error.name || 'Error'}}: ${{error.message || error}}`
      }});
    }}
    await sleep(2000);
    location.reload();
  }}
}})();
</script>""".encode("utf-8")


def build_public_page(display_name):
    return f"""<!doctype html>
<html lang=\"en\">
<head>
  <meta charset=\"utf-8\">
  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">
  <title>{display_name}</title>
  <style>
    html, body {{ margin: 0; min-height: 100%; background: #111; color: #ddd; font-family: Arial, Helvetica, sans-serif; }}
    main {{ min-height: 100vh; display: grid; place-items: center; }}
    img {{ display: block; max-width: 100vw; max-height: 100vh; object-fit: contain; }}
    #status {{ position: fixed; left: 12px; bottom: 10px; padding: 6px 9px; border-radius: 6px; background: rgba(0, 0, 0, 0.62); font-size: 13px; }}
  </style>
</head>
<body>
  <main><img id=\"camera\" alt=\"{display_name}\"></main>
  <div id=\"status\">loading {display_name}...</div>
  <script>
    const img = document.getElementById('camera');
    const status = document.getElementById('status');
    let ok = 0;
    let fail = 0;
    function refresh() {{ img.src = `snapshot.jpg?t=${{Date.now()}}`; }}
    img.onload = () => {{ ok += 1; status.textContent = `{display_name} online - frames ${{ok}}`; }};
    img.onerror = () => {{ fail += 1; status.textContent = `waiting for {display_name} - retry ${{fail}}`; }};
    refresh();
    setInterval(refresh, 500);
  </script>
</body>
</html>"""


class CameraWorker:
    def __init__(self, camera_key, port, profile):
        self.config = CAMERAS[camera_key]
        self.port = port
        self.profile = profile
        self.page = build_capture_page(self.config)

    @staticmethod
    def output_root(camera_key):
        return WWW_ROOT / camera_key

    def snapshot_path(self, camera_key):
        return self.output_root(camera_key) / "snapshot.jpg"

    def status_path(self, camera_key):
        return self.output_root(camera_key) / "worker-status.json"

    def write_status(self, camera_key, payload):
        payload["serverTime"] = time.strftime("%Y-%m-%dT%H:%M:%S%z")
        status_file = self.status_path(camera_key)
        temporary = status_file.with_suffix(".json.tmp")
        temporary.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
        for _ in range(10):
            try:
                os.replace(temporary, status_file)
                break
            except PermissionError:
                time.sleep(0.01)

    def make_handler(self):
        worker = self

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                if self.path.startswith("/health/"):
                    camera_key = self.path.rsplit("/", 1)[-1]
                    status_file = worker.status_path(camera_key)
                    body = status_file.read_bytes() if status_file.exists() else b"{}"
                    self.send_response(200)
                    self.send_header("Content-Type", "application/json; charset=utf-8")
                else:
                    body = worker.page
                    self.send_response(200)
                    self.send_header("Content-Type", "text/html; charset=utf-8")
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)

            def do_POST(self):
                parts = self.path.strip("/").split("/")
                if len(parts) != 2 or parts[0] not in {"frame", "status"}:
                    self.send_error(404)
                    return
                operation, camera_key = parts
                if camera_key != worker.config["key"]:
                    self.send_error(404)
                    return

                size = int(self.headers.get("Content-Length", "0"))
                body = self.rfile.read(size)
                if operation == "frame":
                    if len(body) < 1000 or not body.startswith(b"\xff\xd8"):
                        self.send_error(400, "Invalid JPEG frame")
                        return
                    snapshot = worker.snapshot_path(camera_key)
                    temporary = snapshot.with_suffix(".jpg.tmp")
                    temporary.write_bytes(body)
                    for _ in range(100):
                        try:
                            os.replace(temporary, snapshot)
                            break
                        except PermissionError:
                            time.sleep(0.02)
                else:
                    try:
                        worker.write_status(camera_key, json.loads(body.decode("utf-8")))
                    except (UnicodeDecodeError, json.JSONDecodeError):
                        self.send_error(400, "Invalid status JSON")
                        return

                self.send_response(204)
                self.end_headers()

            def log_message(self, format, *args):
                pass

        return Handler

    def run_server(self):
        server = http.server.ThreadingHTTPServer((HOST, self.port), self.make_handler())
        server.serve_forever()

    def run(self):
        config = self.config
        output_root = self.output_root(config["key"])
        output_root.mkdir(parents=True, exist_ok=True)
        self.snapshot_path(config["key"]).unlink(missing_ok=True)
        (output_root / "index.html").write_text(
            build_public_page(config["display_name"]), encoding="utf-8"
        )
        self.write_status(config["key"], {
            "state": "starting",
            "camera": config["display_name"],
            "cameraKey": config["key"],
        })

        self.profile.mkdir(parents=True, exist_ok=True)
        threading.Thread(target=self.run_server, daemon=True).start()
        edge_arguments = [
            str(EDGE),
            "--headless=new",
            f"--user-data-dir={self.profile}",
            "--use-fake-ui-for-media-stream",
            "--autoplay-policy=no-user-gesture-required",
            "--no-first-run",
            "--disable-gpu",
            f"http://{HOST}:{self.port}/",
        ]

        while True:
            try:
                process = subprocess.Popen(
                    edge_arguments,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
                process.wait()
            except Exception as error:
                self.write_status(config["key"], {
                    "state": "edge-start-error",
                    "error": str(error),
                })
            time.sleep(2)


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--camera", required=True, choices=sorted(CAMERAS))
    parser.add_argument("--port", required=True, type=int)
    parser.add_argument("--profile", required=True, type=pathlib.Path)
    return parser.parse_args()


if __name__ == "__main__":
    arguments = parse_args()
    CameraWorker(arguments.camera, arguments.port, arguments.profile).run()
