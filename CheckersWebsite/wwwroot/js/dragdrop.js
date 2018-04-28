/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
var GameTrueCoords = null;
var GameGrabPoint = null;
var GameDragTarget = null;
var GameGrabScreenCoords = null;
var GameGrabClientCoords = null;
function GameInit() {
    if ($('.board-col').length === 1) {
        $('*').on('mousedown', GameGrab);
        $('*').on('dragend', GameDrop);
    }
}
function GameGrab(evt) {
    var targetElement = evt.target;
    if (!GameDragTarget && targetElement.id.startsWith('piece') && $('.current-player').length === 1 && $('.board').hasClass($(targetElement).attr('player'))) {
        $('.selected').removeClass('selected');
        $('.drag').removeClass('drag');
        $("#" + targetElement.id).addClass('selected drag');
        GameGrabScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        GameGrabClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
        GameDragTarget = targetElement;
        GameGrabPoint = {
            x: evt.clientX,
            y: evt.clientY
        };
    }
}
;
function GameDrop(evt) {
    if (GameDragTarget) {
        GameMovePiece(evt);
    }
    GameDragTarget = null;
}
;
function GameMovePiece(evt) {
    var dropClientCoords = {
        x: GameGrabClientCoords.x + (evt.screenX - GameGrabScreenCoords.x),
        y: GameGrabClientCoords.y + (evt.screenY - GameGrabScreenCoords.y)
    };
    var boundingSquare = GetBoundingSquare(dropClientCoords);
    if (boundingSquare) {
        var coord = boundingSquare.id.replace('square', '');
        var startCoord = evt.target.id.replace('piece', '');
        var row = parseInt(coord[0]);
        var col = parseInt(coord[1]);
        var startRow = parseInt(startCoord[0]);
        var startCol = parseInt(startCoord[1]);
        if (coord === startCoord) {
            return;
        }
        $(GameDragTarget).css('grid-row', "" + (row + 1));
        $(GameDragTarget).css('grid-column', "" + (col + 1));
        boardClick(boundingSquare.id.replace('square', '')[0], boundingSquare.id.replace('square', '')[1]);
    }
}
//# sourceMappingURL=dragdrop.js.map