/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
// Taken with modifications from http://svg-whiz.com/svg/DragAndDrop.svg
var SVGRoot = null;
var TrueCoords = null;
var GrabPoint = null;
var DragTarget = null;
var DragEnded = false;
var GrabScreenCoords = null;
var GrabClientCoords = null;
function Init(rootSelector) {
    SVGRoot = $(rootSelector)[0];
    $(SVGRoot).on('dragover', Drag);
    $(SVGRoot).on('dragend', Drop);
    // these svg points hold x and y values...
    // very handy, but they do not display on the screen (just so you know)
    TrueCoords = SVGRoot.createSVGPoint();
    GrabPoint = $('.board > svg')[0].createSVGPoint();
}
function Grab(evt) {
    console.log('grab');
    // find out which element we moused down on
    var targetElement = evt.target;
    GetTrueCoords(evt);
    console.log($('.selected-add').length);
    console.log($('.current-player').length);
    console.log($('.current-player').hasClass($(targetElement).attr('player')));
    console.log(targetElement.id.startsWith('piece'));
    // you cannot drag the background itself, so ignore any attempts to mouse down on it
    if (!DragTarget && $('.current-player').length === 1 && $('.current-player').hasClass($(targetElement).attr('player')) && targetElement.id.startsWith('piece')) {
        $('.selected').removeClass('selected');
        $("#" + targetElement.id).addClass('selected drag');
        GrabScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        GrabClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };
        console.log(evt);
        targetElement = targetElement.closest('g');
        //set the item moused down on as the element to be dragged
        DragTarget = targetElement;
        // move this element to the \"top\" of the display, so it is (almost)
        // always over other elements (exception: in this case, elements that are
        // \"in the folder\" (children of the folder group) with only maintain
        // hierarchy within that group
        if (DragTarget) {
            $('.board svg')[0].appendChild(DragTarget);
        }
        // turn off all pointer events to the dragged element, this does 2 things:
        // 1) allows us to drag text elements without selecting the text
        // 2) allows us to find out where the dragged element is dropped (see Drop)
        DragTarget.setAttributeNS(null, 'pointer-events', 'none');
        // we need to find the current position and translation of the grabbed element,
        // so that we only apply the differential between the current location
        // and the new location
        var transMatrix = DragTarget.getCTM();
        GrabPoint.x = TrueCoords.x - Number(transMatrix.e);
        GrabPoint.y = TrueCoords.y - Number(transMatrix.f);
    }
}
;
function Drag(evt) {
    console.log('drag');
    // if we don't currently have an element in tow, don't do anything
    if (DragTarget) {
        // account for zooming and panning
        GetTrueCoords(evt);
        // account for the offset between the element's origin and the
        // exact place we grabbed it... this way, the drag will look more natural
        console.log(TrueCoords);
        console.log(GrabPoint);
        var newX = (TrueCoords.x - GrabPoint.x) / $('.board > svg')[0].getBoundingClientRect().width * 50;
        var newY = (TrueCoords.y - GrabPoint.y) / $('.board > svg')[0].getBoundingClientRect().height * 50;
        // apply a new tranform translation to the dragged element, to display
        // it in its new location
        $(DragTarget)[0].setAttribute('transform', 'translate(' + newX + ',' + newY + ')');
    }
}
;
function Drop(evt) {
    console.log('drop');
    console.log(DragTarget);
    // if we aren't currently dragging an element, don't do anything
    if (DragTarget && $('.selected-add').length === 0) {
        // since the element currently being dragged has its pointer-events turned off,
        // we are afforded the opportunity to find out the element it's being dropped on
        var targetElement = evt.target;
        if (evt.type === 'dragend') {
            var dropScreenCoords = {
                x: evt.screenX,
                y: evt.screenY
            };
            var dropClientCoords = {
                x: GrabClientCoords.x + (evt.screenX - GrabScreenCoords.x),
                y: GrabClientCoords.y + (evt.screenY - GrabScreenCoords.y)
            };
            var squares = $('.square');
            var movePiece = $('.board').attr('id') !== 'board-editor-board-container';
            for (var i = 0; i < squares.length; i++) {
                var el = squares[i];
                var boundingRect = el.getBoundingClientRect();
                if (boundingRect.left <= dropClientCoords.x &&
                    boundingRect.right >= dropClientCoords.x &&
                    boundingRect.top <= dropClientCoords.y &&
                    boundingRect.bottom >= dropClientCoords.y) {
                    targetElement = el;
                    var coord = el.id.replace('square', '');
                    var row = parseInt(coord[0]);
                    var col = parseInt(coord[1]);
                    if (movePiece) {
                        boardClick(row, col);
                        $(DragTarget).removeAttr('pointer-events');
                    }
                    else {
                        if ($(targetElement).hasClass('drop-target')) {
                            $(DragTarget).removeAttr('transform');
                            $(DragTarget).find('svg').attr('x', $(targetElement).attr('x'));
                            $(DragTarget).find('svg').attr('y', $(targetElement).attr('y'));
                            $(DragTarget).removeAttr('pointer-events');
                        }
                        else {
                            $(DragTarget).remove();
                        }
                    }
                    break;
                }
            }
        }
        // set the global variable to null, so nothing will be dragged until we
        // grab the next element
        DragTarget = null;
        return;
    }
    if ($('.selected-add').length !== 0) {
        console.log('here');
        var dropClientCoords;
        if (evt.type === 'dragend') {
            var dropScreenCoords = {
                x: evt.screenX,
                y: evt.screenY
            };
            dropClientCoords = {
                x: GrabClientCoords.x + (evt.screenX - GrabScreenCoords.x),
                y: GrabClientCoords.y + (evt.screenY - GrabScreenCoords.y)
            };
        }
        else {
            dropClientCoords = {
                x: evt.clientX,
                y: evt.clientY
            };
        }
        var squares = $('.square');
        for (var i = 0; i < squares.length; i++) {
            var el = squares[i];
            var boundingRect = el.getBoundingClientRect();
            if (boundingRect.left <= dropClientCoords.x &&
                boundingRect.right >= dropClientCoords.x &&
                boundingRect.top <= dropClientCoords.y &&
                boundingRect.bottom >= dropClientCoords.y) {
                console.log('bound');
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
                var coord = el.id.replace('square', '');
                var row = parseInt(coord[0]);
                var col = parseInt(coord[1]);
                var g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
                var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                svg.setAttribute('id', "svg" + row + col);
                svg.setAttribute('onclick', "pieceClick(" + row + col + ")");
                svg.setAttribute('height', '12.5%');
                svg.setAttribute('width', '12.5%');
                svg.setAttribute('style', 'fill:none');
                svg.setAttribute('x', "" + $(el).attr('x'));
                svg.setAttribute('y', "" + $(el).attr('y'));
                var image = document.createElementNS('http://www.w3.org/2000/svg', 'image');
                image.setAttribute('id', "piece" + row + col);
                image.setAttribute('player', player);
                image.setAttribute('height', '100%');
                image.setAttribute('width', '100%');
                image.setAttribute('href', "/images/SteelTheme/" + player + pieceType + ".png");
                var rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
                rect.setAttribute('id', "rect" + row + col);
                rect.setAttribute('class', 'selected-piece-highlight');
                rect.setAttribute('height', '100%');
                rect.setAttribute('width', '100%');
                rect.setAttribute('style', 'fill:none; stroke:goldenrod');
                svg.appendChild(image);
                svg.appendChild(rect);
                g.appendChild(svg);
                $("#piece" + row + col).closest('g').first().remove();
                $('.board > svg')[0].appendChild(g);
                $('.selected-add').removeClass('selected-add');
                break;
            }
        }
    }
}
;
function GetTrueCoords(evt) {
    // find the current zoom level and pan setting, and adjust the reported
    // mouse position accordingly
    var newScale = SVGRoot.currentScale;
    var translation = SVGRoot.currentTranslate;
    TrueCoords.x = (evt.clientX - translation.x) / newScale;
    TrueCoords.y = (evt.clientY - translation.y) / newScale;
}
;
//# sourceMappingURL=dragdrop.js.map