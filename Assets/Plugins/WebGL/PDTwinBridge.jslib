mergeInto(LibraryManager.library, {
  SubmitResult: function(scoreFloat, dataJsonPtr) {
    var dataJson = UTF8ToString(dataJsonPtr);
    window.parent.postMessage(
      {
        type: "PDTWIN_SUBMIT",
        score: scoreFloat,
        data: JSON.parse(dataJson)
      },
      "*"
    );
  }
});
