#!/bin/sh
set -eu

mode="${1:-status}"
serial_link="/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0"

wait_for_http() {
  port="$1"
  path="$2"
  attempts=15
  while [ "$attempts" -gt 0 ]; do
    if curl -fsS --max-time 3 "http://127.0.0.1:${port}${path}" >/dev/null 2>&1; then
      return 0
    fi
    attempts=$((attempts - 1))
    sleep 1
  done
  return 1
}

stop_gateways() {
  sudo systemctl stop pi-gateway-fxplc pi-gateway-hsl
  sleep 1
}

reset_sc09() {
  stop_gateways
  if command -v usbreset >/dev/null 2>&1 && [ -e "$serial_link" ]; then
    sudo usbreset 1a86:7523
    sleep 3
  else
    echo "usbreset or SC09 serial link not found; unplug/replug may be required." >&2
  fi
}

start_hsl() {
  stop_gateways
  sudo systemctl start pi-gateway-hsl
  wait_for_http 5000 /debug
}

start_fxplc() {
  stop_gateways
  sudo systemctl start pi-gateway-fxplc
  wait_for_http 5000 /health
}

case "$mode" in
  hsl)
    start_hsl
    ;;
  fxplc)
    start_fxplc
    ;;
  restart)
    if systemctl is-active --quiet pi-gateway-fxplc; then
      sudo systemctl restart pi-gateway-fxplc
      wait_for_http 5000 /health
    else
      sudo systemctl restart pi-gateway-hsl
      wait_for_http 5000 /debug
    fi
    ;;
  reset-sc09)
    active="hsl"
    if systemctl is-active --quiet pi-gateway-fxplc; then active="fxplc"; fi
    reset_sc09
    if [ "$active" = "fxplc" ]; then start_fxplc; else start_hsl; fi
    ;;
  status)
    ;;
  *)
    echo "Usage: $0 {hsl|fxplc|restart|reset-sc09|status}" >&2
    exit 2
    ;;
esac

echo "HSL:   $(systemctl is-active pi-gateway-hsl || true)"
echo "fxplc: $(systemctl is-active pi-gateway-fxplc || true)"
echo "SC09:  $(readlink -f "$serial_link" 2>/dev/null || echo missing)"
