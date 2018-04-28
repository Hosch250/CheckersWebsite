/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>

// Taken with modifications from http://svg-whiz.com/svg/DragAndDrop.svg

var BoardEditorSVGRoot = null;

var BoardEditorTrueCoords = null;
var BoardEditorGrabPoint = null;
var BoardEditorDragTarget = null;

var BoardEditorGrabScreenCoords = null;
var BoardEditorGrabClientCoords = null;

function BoardEditorInit() {
    BoardEditorSVGRoot = $('#board-editor-svg')[0];

    $(BoardEditorSVGRoot).on('dragover', BoardEditorDrag);
    $(BoardEditorSVGRoot).on('dragend', BoardEditorDrop);
    
    BoardEditorTrueCoords = BoardEditorSVGRoot.createSVGPoint();
    BoardEditorGrabPoint = ($('.board>svg')[0] as any).createSVGPoint();
}

function BoardEditorGrab(evt) {
    var targetElement = evt.target;
    BoardEditorGetTrueCoords(evt);
    
    if (!BoardEditorDragTarget && evt.target.id.startsWith('piece')) {
        if (!$(targetElement).hasClass('selected-add')) {
            $('.selected-add').removeClass('selected-add');
        }

        $('.selected').removeClass('selected');
        $('.drag').removeClass('drag');
        $(`#${targetElement.id}`).addClass('selected drag')
        
        BoardEditorGrabScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        BoardEditorGrabClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
        
        BoardEditorDragTarget = targetElement.closest('g');

        if (BoardEditorDragTarget) {
            $('.board svg')[0].appendChild(BoardEditorDragTarget);
        }

        targetElement.setAttributeNS(null, 'pointer-events', 'none');

        BoardEditorGrabPoint.x = BoardEditorTrueCoords.x;
        BoardEditorGrabPoint.y = BoardEditorTrueCoords.y;
    }
};

function BoardEditorDrag(evt) {
    if (BoardEditorDragTarget) {
        BoardEditorGetTrueCoords(evt);

        var boardBounds = $('.board>svg')[0].getBoundingClientRect();

        var newX = (BoardEditorTrueCoords.x - BoardEditorGrabPoint.x) / boardBounds.width * 50;
        var newY = (BoardEditorTrueCoords.y - BoardEditorGrabPoint.y) / boardBounds.height * 50;
        
        $(BoardEditorDragTarget)[0].setAttribute('transform', 'translate(' + newX + ',' + newY + ')');
    }
};

function BoardEditorDrop(evt) {
    if (BoardEditorDragTarget && $('.selected-add').length === 0) {
        MovePiece(evt);
    } else if ($('.selected-add').length !== 0) {
        AddPieceToBoard(evt);
    }

    $('[pointer-events]').removeAttr('pointer-events');
};

function MovePiece(evt) {
    var dropClientCoords = {
        x: BoardEditorGrabClientCoords.x + (evt.screenX - BoardEditorGrabScreenCoords.x),
        y: BoardEditorGrabClientCoords.y + (evt.screenY - BoardEditorGrabScreenCoords.y)
    };
    var boundingSquare = GetBoundingSquare(dropClientCoords);

    if (boundingSquare) {
        var coord = boundingSquare.id.replace('square', '');
        var row = parseInt(coord[0]);
        var col = parseInt(coord[1]);

        $(`#piece${row}${col}`).closest('g').first().remove();
        $(BoardEditorDragTarget).removeAttr('transform');
        $(BoardEditorDragTarget).find('svg').attr('x', $(boundingSquare).attr('x'));
        $(BoardEditorDragTarget).find('svg').attr('y', $(boundingSquare).attr('y'));

        $(BoardEditorDragTarget).find('svg').attr('id', `svg${row}${col}`);
        $(BoardEditorDragTarget).find('image').attr('id', `piece${row}${col}`);
        $(BoardEditorDragTarget).find('rect').attr('id', `rect${row}${col}`);
        $(BoardEditorDragTarget).find('svg').attr('onclick', `pieceClick(${row}, ${col})`);
    } else {
        $(BoardEditorDragTarget).remove();
    }

    BoardEditorDragTarget = null;
}

function AddPieceToBoard(evt) {
    var dropClientCoords: { x; y };
    if (evt.type === 'dragend') {
        var dropScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        dropClientCoords = {
            x: BoardEditorGrabClientCoords.x + (evt.screenX - BoardEditorGrabScreenCoords.x),
            y: BoardEditorGrabClientCoords.y + (evt.screenY - BoardEditorGrabScreenCoords.y)
        };
    } else {
        dropClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
    }

    var boundingSquare = GetBoundingSquare(dropClientCoords);
    if (boundingSquare) {
        var player: string;
        var pieceType: string;
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

        var g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('id', `svg${row}${col}`);
        svg.setAttribute('onclick', `pieceClick(${row}${col})`);
        svg.setAttribute('height', '12.5%');
        svg.setAttribute('width', '12.5%');
        svg.setAttribute('style', 'fill:none');
        svg.setAttribute('x', `${$(boundingSquare).attr('x')}`);
        svg.setAttribute('y', `${$(boundingSquare).attr('y')}`);

        var image = document.createElementNS('http://www.w3.org/2000/svg', 'image');
        image.setAttribute('id', `piece${row}${col}`);
        image.setAttribute('height', '100%');
        image.setAttribute('width', '100%');
        image.setAttribute('href', `/images/SteelTheme/${player}${pieceType}.png`);

        var rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('id', `rect${row}${col}`);
        rect.setAttribute('class', 'selected-piece-highlight');
        rect.setAttribute('height', '100%');
        rect.setAttribute('width', '100%');
        rect.setAttribute('style', 'fill:none; stroke:goldenrod');

        svg.appendChild(image);
        svg.appendChild(rect);
        g.appendChild(svg);

        $(`#piece${row}${col}`).closest('g').first().remove();
        $('.board>svg')[0].appendChild(g);
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

function BoardEditorGetTrueCoords(evt) {    
    BoardEditorTrueCoords.x = evt.clientX;
    BoardEditorTrueCoords.y = evt.clientY;
};