mergeInto(LibraryManager.library, {
  SubmitResult: function(scoreFloat, detailsPtr) {
    var details = UTF8ToString(detailsPtr);
    window.parent.postMessage(
      {
        type: "PDTWIN_SUBMIT",
        score: scoreFloat,
        details: details
      },
      window.location.origin
    );
  },

  ReportProgressResult: function(scoreFloat) {
    window.parent.postMessage(
      {
        type: "PDTWIN_PROGRESS",
        score: scoreFloat
      },
      window.location.origin
    );
  }
});
