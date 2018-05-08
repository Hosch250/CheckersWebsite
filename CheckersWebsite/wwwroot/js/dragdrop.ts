/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>

var GameTrueCoords = null;
var GameGrabPoint = null;
var GameDragTarget = null;

var GameGrabScreenCoords = null;
var GameGrabClientCoords = null;

function GameInit() {
    if ($('.board-col').length === 1) {
        $('*').on('mousedown', GameGrab);
        $('*').on('keydown', GameKeyPress);
        $('*').on('dragend', GameDrop);
        $('*').on('mouseup', GameDrop);
        $('*').on('click', GameClick);
    }
}

function GameKeyPress(evt) {
    if (evt.keyCode !== 32 && evt.keyCode !== 13) {
        return true;
    }

    if (GameDragTarget) {
        GameDrop(evt);
        return false;
    } else {
        GameGrab(evt);
        $('.drag').removeClass('drag');
        return false;
    }
}

function GameGrab(evt) {
    var targetElement = evt.target;

    if (!GameDragTarget && targetElement.id.startsWith('piece') && $('.current-player').length === 1 && $('.board').hasClass($(targetElement).attr('player'))) {
        $('.selected').removeClass('selected');
        $('.drag').removeClass('drag');
        $(`#${targetElement.id}`).addClass('selected drag')

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
};

function GameClick(evt) {
    if (!GameDragTarget && evt.target.id.startsWith('square') && $('.selected').length !== 0) {
        GameDragTarget = $('.selected')[0];
        GameDrop(evt);
        return false;
    }

    return true;
}

function GameDrop(evt) {
    if (GameDragTarget) {
        GameMovePiece(evt);
    }

    GameDragTarget = null;
};

function GameMovePiece(evt) {
    var dropClientCoords = {
        x: GameGrabClientCoords.x + (evt.screenX - GameGrabScreenCoords.x),
        y: GameGrabClientCoords.y + (evt.screenY - GameGrabScreenCoords.y)
    };
    var boundingSquare = getGameBoundingSquare(evt);

    if (boundingSquare) {
        var coord = boundingSquare.id.replace('square', '');
        var startCoord = evt.target.id.replace('piece', '');

        var row = parseInt(coord[0]);
        var col = parseInt(coord[1]);

        var startRow = parseInt(startCoord[0]);
        var startCol = parseInt(startCoord[1]);

        if (coord === startCoord) {
            $('.drag').removeClass('drag');
            return;
        }

        $(GameDragTarget).css('grid-row', `${row + 1}`);
        $(GameDragTarget).css('grid-column', `${col + 1}`);
        
        boardClick(boundingSquare.id.replace('square', '')[0], boundingSquare.id.replace('square', '')[1]);
    }
}

function getGameBoundingSquare(evt) {
    var dropClientCoords: { x; y };
    if (evt.type === 'dragend') {
        var dropScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        dropClientCoords = {
            x: GameGrabClientCoords.x + (evt.screenX - GameGrabScreenCoords.x),
            y: GameGrabClientCoords.y + (evt.screenY - GameGrabScreenCoords.y)
        };
    } else if (evt.type === 'keydown') {
        return $('#' + evt.target.id.replace('piece', 'square'))[0];
    } else {
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