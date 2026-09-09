// =================== SCORM 1.2 Minimal Wrapper ===================
var SCORM = (function () {
  var apiHandle = null;
  var initialized = false;
  var unityInstance = null; // Reference to Unity instance for callbacks

  // Try to locate SCORM API in LMS
  function findAPI(win) {
    var maxTries = 7, tries = 0;
    while ((win.API == null) && (win.parent != null) && (win.parent != win)) {
      tries++;
      if (tries > maxTries) {
        console.log("Error finding API -- too deeply nested");
        return null;
      }
      win = win.parent;
    }
    return win.API;
  }

  function getAPI() {
    if (apiHandle) return apiHandle;
    var api = findAPI(window);
    if (!api && window.opener) api = findAPI(window.opener);
    if (!api) console.log("Unable to find SCORM API");
    apiHandle = api;
    return apiHandle;
  }

  function initialize() {
    if (initialized) return "true";
    var api = getAPI();
    if (api && typeof api.LMSInitialize === "function") {
      var result = api.LMSInitialize("");
      initialized = result.toString() === "true";
      console.log("SCORM Initialize:", result);
      return result.toString();
    }
    return "false";
  }

  function finish() {
    var api = getAPI();
    if (api && typeof api.LMSFinish === "function") {
      var result = api.LMSFinish("");
      console.log("SCORM Finish:", result);
      initialized = false;
      return result.toString();
    }
    return "false";
  }

  function setValue(element, value) {
    var api = getAPI();
    if (api && typeof api.LMSSetValue === "function") {
      var result = api.LMSSetValue(element, value);
      if (typeof api.LMSCommit === "function") api.LMSCommit("");
      console.log("SCORM SetValue:", element, value, "Result:", result);
      return result.toString();
    }
    return "false";
  }

  function getValue(element) {
    var api = getAPI();
    if (api && typeof api.LMSGetValue === "function") {
      var value = api.LMSGetValue(element);
      console.log("SCORM GetValue:", element, value);
      return value != null ? value.toString() : "";
    }
    return "";
  }

  // Handle incoming messages (from Unity iframe or others)
  function handleMessage(event) {
    var data = event.data;

    // 🔑 Handle unityReady message from iframe
    if (data && typeof data === "object" && data.type === "unityReady") {
      // Store a reference to the iframe as unityInstance proxy
      var iframe = document.getElementById("gameFrame");
      if (iframe && iframe.contentWindow) {
        unityInstance = iframe.contentWindow;
        console.log("Unity iframe registered with SCORM wrapper.");
      }
      return;
    }

    // Handle specific SCORM-related commands
    if (data === "initSCORM") {
      initialize();
    } else if (typeof data === "string" && data.startsWith("setScore:")) {
      var score = parseInt(data.split(":")[1]);
      if (!isNaN(score)) {
        setValue("cmi.core.score.min", "0");
        setValue("cmi.core.score.max", "100");
        setValue("cmi.core.score.raw", score);
        setValue("cmi.core.lesson_status", (score >= 50) ? "passed" : "failed");
      }
    } else if (typeof data === "string" && data.startsWith("setStatus:")) {
      var status = data.split(":")[1];
      setValue("cmi.core.lesson_status", status);
    } else if (data === "setStatusCompleted") {
      setValue("cmi.core.lesson_status", "completed");
    } else if (data === "markFinished") {
      finish();
    } else if (data === "requestStudentInfo") {
      var studentId = getValue("cmi.core.student_id");
      var studentName = getValue("cmi.core.student_name");
      var iframe = document.getElementById("gameFrame");
      if (iframe && iframe.contentWindow) {
        // Send student info via postMessage to iframe
        iframe.contentWindow.postMessage({
          type: "studentInfo",
          id: studentId,
          name: studentName
        }, "*");
        console.log("Student info sent to Unity iframe:", studentId, studentName);
      } else {
        console.log("Unity iframe not yet available to send student info.");
      }
    }
  }

  // Listen for messages from iframe or parent window
  window.addEventListener("message", handleMessage);

  // Return public API
  return {
    initialize,
    finish,
    setValue,
    getValue,
    setUnityInstance: function (instance) {
      unityInstance = instance;
    },
    handleMessage
  };
})();

// Auto-initialize SCORM on page load
window.addEventListener("load", function () {
  var initResult = SCORM.initialize();
  console.log("SCORM initialized:", initResult);
});
