var docViewer;

function getDocViewer(){
    if(docViewer)
        return docViewer;
    else if(window.FlexPaperViewer_Annotations)
        return window.FlexPaperViewer_Annotations;
    else if(document.FlexPaperViewer_Annotations)
	return document.FlexPaperViewer_Annotations;
    else 
	return null;
}

/**
 * Handles the event of annotations getting clicked. 
 *
 * @example onMarkClicked(object)
 *
 * @param Object mark that was clicked
 */
function onMarkClicked(mark){
}

/**
 * Handles the event of annotations getting clicked. 
 *
 * @example onMarkCreated(object)
 *
 * @param Object mark that was created
 */
function onMarkCreated(mark){
}

/**
 * Handles the event of external links getting clicked in the document. 
 *
 * @example onExternalLinkClicked("http://www.google.com")
 *
 * @param String link
 */
function onExternalLinkClicked(link){
   // alert("link " + link + " clicked" );
   window.location.href = link;
}

/**
 * Recieves progress information about the document being loaded
 *
 * @example onProgress( 100,10000 );
 *
 * @param int loaded
 * @param int total
 */
function onProgress(loadedBytes,totalBytes){
}

/**
 * Handles the event of a document is in progress of loading
 *
 */
function onDocumentLoading(){
}

/**
 * Receives messages about the current page being changed
 *
 * @example onCurrentPageChanged( 10 );
 *
 * @param int pagenum
 */
function onCurrentPageChanged(pagenum){
}

/**
 * Receives messages about the document being loaded
 *
 * @example onDocumentLoaded( 20 );
 *
 * @param int totalPages
 */
function onDocumentLoaded(totalPages){
	getDocViewer().addMark({note: $('#txt_addmark_note').val(),positionX:200,positionY:200,width:200,height:300,pageIndex:1});
}

/**
 * Receives error messages when a document is not loading properly
 *
 * @example onDocumentLoadedError( "Network error" );
 *
 * @param String errorMessage
 */
function onDocumentLoadedError(errMessage){
}