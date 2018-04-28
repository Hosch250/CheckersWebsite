/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
var BoardEditorTrueCoords = null;
var BoardEditorGrabPoint = null;
var BoardEditorDragTarget = null;
var BoardEditorGrabScreenCoords = null;
var BoardEditorGrabClientCoords = null;
function BoardEditorInit() {
    if ($('.board-editor').length === 1) {
        $('*').on('mousedown', BoardEditorGrab);
        $('*').on('dragend', BoardEditorDrop);
    }
}
function BoardEditorGrab(evt) {
    var targetElement = evt.target;
    if (!BoardEditorDragTarget && evt.target.id.startsWith('piece')) {
        if (!$(targetElement).hasClass('selected-add')) {
            $('.selected-add').removeClass('selected-add');
        }
        $('.selected').removeClass('selected');
        $('.drag').removeClass('drag');
        $("#" + targetElement.id).addClass('selected drag');
        BoardEditorGrabScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        BoardEditorGrabClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
        BoardEditorDragTarget = targetElement;
        BoardEditorGrabPoint = {
            x: evt.clientX,
            y: evt.clientY
        };
    }
}
;
function BoardEditorDrop(evt) {
    if (BoardEditorDragTarget && $('.selected-add').length === 0) {
        BoardEditorMovePiece(evt);
        GetFEN();
    }
    else if ($('.selected-add').length !== 0) {
        BoardEditorAddPieceToBoard(evt);
        GetFEN();
    }
    BoardEditorDragTarget = null;
}
;
function BoardEditorMovePiece(evt) {
    var dropClientCoords = {
        x: BoardEditorGrabClientCoords.x + (evt.screenX - BoardEditorGrabScreenCoords.x),
        y: BoardEditorGrabClientCoords.y + (evt.screenY - BoardEditorGrabScreenCoords.y)
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
        $("#piece" + row + col).remove();
        $(BoardEditorDragTarget).attr('id', "piece" + row + col);
        $(BoardEditorDragTarget).attr('onmousedown', "pieceClick(" + row + ", " + col + ")");
        $(BoardEditorDragTarget).css('grid-row', "" + (row + 1));
        $(BoardEditorDragTarget).css('grid-column', "" + (col + 1));
    }
    else {
        $(BoardEditorDragTarget).remove();
    }
}
function BoardEditorAddPieceToBoard(evt) {
    var dropClientCoords;
    if (evt.type === 'dragend') {
        var dropScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        dropClientCoords = {
            x: BoardEditorGrabClientCoords.x + (evt.screenX - BoardEditorGrabScreenCoords.x),
            y: BoardEditorGrabClientCoords.y + (evt.screenY - BoardEditorGrabScreenCoords.y)
        };
    }
    else {
        dropClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
    }
    var boundingSquare = GetBoundingSquare(dropClientCoords);
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
        var newPiece = "<img id=\"piece" + row + col + "\" class=\"piece\" player=\"" + player + "\" pieceType=\"" + pieceType + "\" onmousedown=\"pieceClick(" + row + ", " + col + ")\" src=\"/images/SteelTheme/" + player + pieceType + ".png\" style=\"grid-row: " + (row + 1) + "; grid-column: " + (col + 1) + "\" />";
        $('.board').append(newPiece);
        $('.selected-add').removeClass('selected-add');
    }
}
function GetBoundingSquare(dropClientCoords) {
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