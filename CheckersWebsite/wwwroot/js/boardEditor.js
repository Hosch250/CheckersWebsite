/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
var boardEditorTrueCoords = null;
var boardEditorGrabPoint = null;
var boardEditorDragTarget = null;
var boardEditorGrabScreenCoords = null;
var boardEditorGrabClientCoords = null;
function boardEditorInit() {
    if ($('.board-editor').length === 1) {
        $('*').on('mousedown', boardEditorGrab);
        $('*').on('keydown', boardEditorKeyPress);
        $('*').on('dragend', boardEditorDrop);
        $('*').on('mouseup', boardEditorDrop);
        $('*').on('click', boardEditorClick);
    }
}
function boardEditorKeyPress(evt) {
    if (evt.keyCode !== 32 && evt.keyCode !== 13) {
        return true;
    }
    if (boardEditorDragTarget && $('.selected-add').length === 0 || $('.selected-add').length !== 0) {
        boardEditorDrop(evt);
        GetFEN();
        return false;
    }
    else {
        boardEditorGrab(evt);
        $('.drag').removeClass('drag');
        return false;
    }
}
function boardEditorGrab(evt) {
    var targetElement = evt.target;
    if (!boardEditorDragTarget && evt.target.id.startsWith('piece')) {
        if (!$(targetElement).hasClass('selected-add')) {
            $('.selected-add').removeClass('selected-add');
        }
        $('.selected').removeClass('selected');
        $('.drag').removeClass('drag');
        if (!$(targetElement).hasClass('template-piece')) {
            $("#" + targetElement.id).addClass('selected drag');
        }
        boardEditorGrabScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        boardEditorGrabClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
        boardEditorDragTarget = targetElement;
        boardEditorGrabPoint = {
            x: evt.clientX,
            y: evt.clientY
        };
    }
}
;
function boardEditorClick(evt) {
    if (!boardEditorDragTarget && evt.target.id.startsWith('square') && $('.selected').length !== 0) {
        boardEditorDragTarget = $('.selected')[0];
        boardEditorDrop(evt);
        return false;
    }
    return true;
}
function boardEditorDrop(evt) {
    if (boardEditorDragTarget && $('.selected-add').length === 0) {
        boardEditorMovePiece(evt);
        GetFEN();
    }
    else if ($('.selected-add').length !== 0) {
        boardEditorAddPieceToBoard(evt);
        GetFEN();
    }
    boardEditorDragTarget = null;
}
;
function boardEditorMovePiece(evt) {
    var boundingSquare = getBoundingSquare(evt);
    if (boundingSquare) {
        var coord = boundingSquare.id.replace('square', '');
        var startCoord = boardEditorDragTarget.id.replace('piece', '');
        var row = parseInt(coord[0]);
        var col = parseInt(coord[1]);
        var startRow = parseInt(startCoord[0]);
        var startCol = parseInt(startCoord[1]);
        if (coord === startCoord) {
            $('.drag').removeClass('drag');
            return;
        }
        $("#piece" + row + col).remove();
        $(boardEditorDragTarget).attr('id', "piece" + row + col);
        $(boardEditorDragTarget).css('grid-row', "" + (row + 1));
        $(boardEditorDragTarget).css('grid-column', "" + (col + 1));
    }
    else {
        $(boardEditorDragTarget).remove();
    }
}
function boardEditorAddPieceToBoard(evt) {
    var boundingSquare = getBoundingSquare(evt);
    if (boundingSquare) {
        var player;
        var pieceType;
        switch ($('.selected-add').attr('id')) {
            case 'piece-black-checker':
                player = "Black";
                pieceType = "Checker";
                break;
            case 'piece-black-king':
                player = "Black";
                pieceType = "King";
                break;
            case 'piece-white-checker':
                player = "White";
                pieceType = "Checker";
                break;
            case 'piece-white-king':
                player = "White";
                pieceType = "King";
                break;
        }
        var coord = boundingSquare.id.replace('square', '');
        var row = parseInt(coord[0]);
        var col = parseInt(coord[1]);
        $("#piece" + row + col).remove();
        var newPiece = "<img id=\"piece" + row + col + "\" class=\"piece\" player=\"" + player + "\" pieceType=\"" + pieceType + "\" src=\"/images/SteelTheme/" + player + pieceType + ".png\" style=\"grid-row: " + (row + 1) + "; grid-column: " + (col + 1) + "\" />";
        $('.board').append(newPiece);
        $('.selected-add').removeClass('selected-add');
    }
}
function getBoundingSquare(evt) {
    var dropClientCoords;
    if (evt.type === 'dragend') {
        var dropScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        dropClientCoords = {
            x: boardEditorGrabClientCoords.x + (evt.screenX - boardEditorGrabScreenCoords.x),
            y: boardEditorGrabClientCoords.y + (evt.screenY - boardEditorGrabScreenCoords.y)
        };
    }
    else if (evt.type === 'keydown') {
        return $('#' + evt.target.id.replace('piece', 'square'))[0];
    }
    else {
        dropClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
    }
    var squares = $('.drop-target');
    for (var i = 0; i < squares.length; i++) {
        var el = squares[i];
        var boundingRect = el.getBoundingClientRect();
        if (boundingRect.left <= dropClientCoords.x &&
            boundingRect.right >= dropClientCoords.x &&
            boundingRect.top <= dropClientCoords.y &&
            boundingRect.bottom >= dropClientCoords.y) {
            return el;
        }
    }
    return null;
}
//# sourceMappingURL=boardEditor.js.map