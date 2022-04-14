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

function appendLog(val){
	$("#txt_eventlog").val(val + '\n' + $("#txt_eventlog").val());
}

/**
 * Handles the event of annotations getting clicked. 
 *
 * @example onMarkClicked(object)
 *
 * @param Object mark that was clicked
 */
function onMarkClicked(mark){
	appendLog('onMarkClicked:' + mark);
}

/**
 * Handles the event of annotations getting clicked. 
 *
 * @example onMarkCreated(object)
 *
 * @param Object mark that was created
 */
function onMarkCreated(mark){
	appendLog('onMarkCreated:' + mark);
}

/**
 * Handles the event of annotations getting clicked. 
 *
 * @example onMarkDeleted(object)
 *
 * @param Object mark that was deleted
 */
function onMarkDeleted(mark){
	appendLog('onMarkDeleted:' + mark);
}

/**
 * Handles the event of annotations getting clicked. 
 *
 * @example onMarkChanged(object)
 *
 * @param Object mark that was changed
 */
function onMarkChanged(mark){
	appendLog('onMarkChanged:' + mark);
}

/**
 * Handles the event of external links getting clicked in the document. 
 *
 * @example onExternalLinkClicked("http://www.google.com")
 *
 * @param String link
 */
function onExternalLinkClicked(link){
	$("#txt_eventlog").val('onExternalLinkClicked' + '\n' + $("#txt_eventlog").val());	
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
	$("#txt_progress").val('onProgress:' + loadedBytes + '/' + totalBytes + '\n');	
}

/**
 * Handles the event of a document is in progress of loading
 *
 */
function onDocumentLoading(){
	$("#txt_eventlog").val('onDocumentLoading' + '\n' + $("#txt_eventlog").val());	
}

/**
 * Receives messages about the current page being changed
 *
 * @example onCurrentPageChanged( 10 );
 *
 * @param int pagenum
 */
function onCurrentPageChanged(pagenum){
	$("#txt_eventlog").val('onCurrentPageChanged:' + pagenum + '\n' + $("#txt_eventlog").val());
}

/**
 * Receives messages about the document being loaded
 *
 * @example onDocumentLoaded( 20 );
 *
 * @param int totalPages
 */
function onDocumentLoaded(totalPages){
	$("#txt_eventlog").val('onDocumentLoaded:' + totalPages + '\n' + $("#txt_eventlog").val());
	var searchterm = $('#txt_searchtext').val();
	if (searchterm.length != 0) {
	    //alert('You are searching for the term \"' + searchterm + '\"' );
	    getDocViewer().searchText($('#txt_searchtext').val())

	}
	//setTimeout('chainOfMethods()', 2000);
	//chainOfMethods();
}

/**
 * Receives error messages when a document is not loading properly
 *
 * @example onDocumentLoadedError( "Network error" );
 *
 * @param String errorMessage
 */
function onDocumentLoadedError(errMessage){
	$("#txt_eventlog").val('onDocumentLoadedError:' + errMessage + '\n' + $("#txt_eventlog").val());
}

function chainOfMethods(){
	getDocViewer().addMark({note: 'sample annotation from gtcs',positionX:-20,positionY:330,width:200,height:180,pageIndex:1,collapsed:false,readonly:false});
	getDocViewer().addMark({note: 'The plug-in features a full set of API functions which can be used to interact with the viewer so that annotations can be stored and recreated later.',positionX:450,positionY:150,width:200,height:180,pageIndex:1,collapsed:false});
	getDocViewer().addMark({selection_info: '1;0;20',has_selection:false,color:'#fffc15'});
	getDocViewer().addMark({selection_info: '3;23;58',has_selection:false,color:'#fffc15'});
	getDocViewer().addMark({ note: 'The plug-in features a full set of API functions which can be used to interact with the viewer so that annotations can be stored and recreated later.', positionX: 300, positionY: 350, width: 200, height: 180, pageIndex: 3, collapsed: true });

	getDocViewer().getMarkList();
	
	var mark1 = new Object();
	mark1.selection_info = '5;9;195';
	mark1.color =  "#fffc15";
	
	var mark2 = new Object();
	mark2.selection_info = '5;581;767';
	mark2.color =  "#fffc15";

	var sampleList = new Array(mark1,mark2);
	
	getDocViewer().addMarks(sampleList);
}