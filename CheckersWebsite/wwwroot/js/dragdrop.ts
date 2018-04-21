/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>

// Taken with modifications from http://svg-whiz.com/svg/DragAndDrop.svg

var SVGRoot = null;

var TrueCoords = null;
var GrabPoint = null;
var DragTarget = null;
var DragEnded = false;

var GrabScreenCoords = null;
var GrabClientCoords = null;

function Init() {
    SVGRoot = $('.board svg')[0];

    $(SVGRoot).on('dragover', Drag);
    $(SVGRoot).on('dragend', Drop);

    // these svg points hold x and y values...
    // very handy, but they do not display on the screen (just so you know)
    TrueCoords = SVGRoot.createSVGPoint();
    GrabPoint = SVGRoot.createSVGPoint();
}

function Grab(evt) {
    // find out which element we moused down on
    var targetElement = evt.target;
    GetTrueCoords(evt);

    // you cannot drag the background itself, so ignore any attempts to mouse down on it
    if ($('.current-player').length === 1 && $('.current-player').hasClass($(targetElement).attr('player')) && targetElement.id.startsWith('piece')) {
        $('.selected').removeClass('selected');
        $(`#${targetElement.id}`).addClass('selected drag')
        
        GrabScreenCoords = {
            x: evt.screenX,
            y: evt.screenY
        };
        GrabClientCoords = {
            x: evt.clientX,
            y: evt.clientY
        };

        targetElement = targetElement.closest('g');
        //set the item moused down on as the element to be dragged
        DragTarget = targetElement;

        // move this element to the \"top\" of the display, so it is (almost)
        // always over other elements (exception: in this case, elements that are
        // \"in the folder\" (children of the folder group) with only maintain
        // hierarchy within that group
        SVGRoot.appendChild(DragTarget);

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
};

function Drag(evt) {
    // if we don't currently have an element in tow, don't do anything
    if (DragTarget) {

        // account for zooming and panning
        GetTrueCoords(evt);

        //console.log(TrueCoords);
        //console.log(GrabPoint);
        //console.log(SVGRoot.getBoundingClientRect());

        // account for the offset between the element's origin and the
        // exact place we grabbed it... this way, the drag will look more natural
        var newX = (TrueCoords.x - GrabPoint.x) / SVGRoot.getBoundingClientRect().width * 50;
        var newY = (TrueCoords.y - GrabPoint.y) / SVGRoot.getBoundingClientRect().height * 50;

        // apply a new tranform translation to the dragged element, to display
        // it in its new location
        $(DragTarget)[0].setAttribute('transform', 'translate(' + newX + ',' + newY + ')');
    }
};

function Drop(evt) {
    // if we aren't currently dragging an element, don't do anything
    if (DragTarget) {
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
            for (var i = 0; i < squares.length; i++) {
                var el = squares[i];

                var boundingRect = el.getBoundingClientRect();

                if (boundingRect.left <= dropClientCoords.x &&
                    boundingRect.right >= dropClientCoords.x &&
                    boundingRect.top <= dropClientCoords.y &&
                    boundingRect.bottom >= dropClientCoords.y) {

                    var coord = el.id.replace('square', '');
                    boardClick(parseInt(coord[0]), parseInt(coord[1]));
                    break;
                }
            }
        }

        // turn the pointer-events back on, so we can grab this item later
        $(DragTarget).removeAttr('pointer-events');

        // set the global variable to null, so nothing will be dragged until we
        // grab the next element
        DragTarget = null;
    }
};

function GetTrueCoords(evt) {
    // find the current zoom level and pan setting, and adjust the reported
    // mouse position accordingly
    var newScale = SVGRoot.currentScale;
    var translation = SVGRoot.currentTranslate;
    
    TrueCoords.x = (evt.clientX - translation.x) / newScale;
    TrueCoords.y = (evt.clientY - translation.y) / newScale;
};