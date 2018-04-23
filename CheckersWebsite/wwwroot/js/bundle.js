/**
* jquery-match-height master by @liabru
* http://brm.io/jquery-match-height/
* License: MIT
*/

;(function(factory) { // eslint-disable-line no-extra-semi
    'use strict';
    if (typeof define === 'function' && define.amd) {
        // AMD
        define(['jquery'], factory);
    } else if (typeof module !== 'undefined' && module.exports) {
        // CommonJS
        module.exports = factory(require('jquery'));
    } else {
        // Global
        factory(jQuery);
    }
})(function($) {
    /*
    *  internal
    */

    var _previousResizeWidth = -1,
        _updateTimeout = -1;

    /*
    *  _parse
    *  value parse utility function
    */

    var _parse = function(value) {
        // parse value and convert NaN to 0
        return parseFloat(value) || 0;
    };

    /*
    *  _rows
    *  utility function returns array of jQuery selections representing each row
    *  (as displayed after float wrapping applied by browser)
    */

    var _rows = function(elements) {
        var tolerance = 1,
            $elements = $(elements),
            lastTop = null,
            rows = [];

        // group elements by their top position
        $elements.each(function(){
            var $that = $(this),
                top = $that.offset().top - _parse($that.css('margin-top')),
                lastRow = rows.length > 0 ? rows[rows.length - 1] : null;

            if (lastRow === null) {
                // first item on the row, so just push it
                rows.push($that);
            } else {
                // if the row top is the same, add to the row group
                if (Math.floor(Math.abs(lastTop - top)) <= tolerance) {
                    rows[rows.length - 1] = lastRow.add($that);
                } else {
                    // otherwise start a new row group
                    rows.push($that);
                }
            }

            // keep track of the last row top
            lastTop = top;
        });

        return rows;
    };

    /*
    *  _parseOptions
    *  handle plugin options
    */

    var _parseOptions = function(options) {
        var opts = {
            byRow: true,
            property: 'height',
            target: null,
            remove: false,
            subtractFromTarget: 0
        };

        if (typeof options === 'object') {
            return $.extend(opts, options);
        }

        if (typeof options === 'boolean') {
            opts.byRow = options;
        } else if (options === 'remove') {
            opts.remove = true;
        }

        return opts;
    };

    /*
    *  matchHeight
    *  plugin definition
    */

    var matchHeight = $.fn.matchHeight = function(options) {
        var opts = _parseOptions(options);

        // handle remove
        if (opts.remove) {
            var that = this;

            // remove fixed height from all selected elements
            this.css(opts.property, '');

            // remove selected elements from all groups
            $.each(matchHeight._groups, function(key, group) {
                group.elements = group.elements.not(that);
            });

            // TODO: cleanup empty groups

            return this;
        }

        if (this.length <= 1 && !opts.target) {
            return this;
        }

        // keep track of this group so we can re-apply later on load and resize events
        matchHeight._groups.push({
            elements: this,
            options: opts
        });

        // match each element's height to the tallest element in the selection
        matchHeight._apply(this, opts);

        return this;
    };

    /*
    *  plugin global options
    */

    matchHeight.version = 'master';
    matchHeight._groups = [];
    matchHeight._throttle = 80;
    matchHeight._maintainScroll = false;
    matchHeight._beforeUpdate = null;
    matchHeight._afterUpdate = null;
    matchHeight._rows = _rows;
    matchHeight._parse = _parse;
    matchHeight._parseOptions = _parseOptions;

    /*
    *  matchHeight._apply
    *  apply matchHeight to given elements
    */

    matchHeight._apply = function(elements, options) {
        var opts = _parseOptions(options),
            $elements = $(elements),
            rows = [$elements];

        // take note of scroll position
        var scrollTop = $(window).scrollTop(),
            htmlHeight = $('html').outerHeight(true);

        // get hidden parents
        var $hiddenParents = $elements.parents().filter(':hidden');

        // cache the original inline style
        $hiddenParents.each(function() {
            var $that = $(this);
            $that.data('style-cache', $that.attr('style'));
        });

        // temporarily must force hidden parents visible
        $hiddenParents.css('display', 'block');

        // get rows if using byRow, otherwise assume one row
        if (opts.byRow && !opts.target) {

            // must first force an arbitrary equal height so floating elements break evenly
            $elements.each(function() {
                var $that = $(this),
                    display = $that.css('display');

                // temporarily force a usable display value
                if (display !== 'inline-block' && display !== 'flex' && display !== 'inline-flex') {
                    display = 'block';
                }

                // cache the original inline style
                $that.data('style-cache', $that.attr('style'));

                $that.css({
                    'display': display,
                    'padding-top': '0',
                    'padding-bottom': '0',
                    'margin-top': '0',
                    'margin-bottom': '0',
                    'border-top-width': '0',
                    'border-bottom-width': '0',
                    'height': '100px',
                    'overflow': 'hidden'
                });
            });

            // get the array of rows (based on element top position)
            rows = _rows($elements);

            // revert original inline styles
            $elements.each(function() {
                var $that = $(this);
                $that.attr('style', $that.data('style-cache') || '');
            });
        }

        $.each(rows, function(key, row) {
            var $row = $(row),
                targetHeight = 0;

            if (!opts.target) {
                // skip apply to rows with only one item
                if (opts.byRow && $row.length <= 1) {
                    $row.css(opts.property, '');
                    return;
                }

                // iterate the row and find the max height
                $row.each(function(){
                    var $that = $(this),
                        style = $that.attr('style'),
                        display = $that.css('display');

                    // temporarily force a usable display value
                    if (display !== 'inline-block' && display !== 'flex' && display !== 'inline-flex') {
                        display = 'block';
                    }

                    // ensure we get the correct actual height (and not a previously set height value)
                    var css = { 'display': display };
                    css[opts.property] = '';
                    $that.css(css);

                    // find the max height (including padding, but not margin)
                    if ($that.outerHeight(false) > targetHeight) {
                        targetHeight = $that.outerHeight(false);
                    }

                    // revert styles
                    if (style) {
                        $that.attr('style', style);
                    } else {
                        $that.css('display', '');
                    }
                });
            } else {
                // if target set, use the height of the target element
                targetHeight = opts.target.outerHeight(false);
            }

            // iterate the row and apply the height to all elements
            $row.each(function(){
                var $that = $(this),
                    verticalPadding = 0;

                // don't apply to a target
                if (opts.target && $that.is(opts.target)) {
                    return;
                }

                // handle padding and border correctly (required when not using border-box)
                if ($that.css('box-sizing') !== 'border-box') {
                    verticalPadding += _parse($that.css('border-top-width')) + _parse($that.css('border-bottom-width'));
                    verticalPadding += _parse($that.css('padding-top')) + _parse($that.css('padding-bottom'));
                }

                // set the height (accounting for padding and border)
                $that.css(opts.property, (targetHeight - verticalPadding - opts.subtractFromTarget) + 'px');
            });
        });

        // revert hidden parents
        $hiddenParents.each(function() {
            var $that = $(this);
            $that.attr('style', $that.data('style-cache') || null);
        });

        // restore scroll position if enabled
        if (matchHeight._maintainScroll) {
            $(window).scrollTop((scrollTop / htmlHeight) * $('html').outerHeight(true));
        }

        return this;
    };

    /*
    *  matchHeight._applyDataApi
    *  applies matchHeight to all elements with a data-match-height attribute
    */

    matchHeight._applyDataApi = function() {
        var groups = {};

        // generate groups by their groupId set by elements using data-match-height
        $('[data-match-height], [data-mh]').each(function() {
            var $this = $(this),
                groupId = $this.attr('data-mh') || $this.attr('data-match-height');

            if (groupId in groups) {
                groups[groupId] = groups[groupId].add($this);
            } else {
                groups[groupId] = $this;
            }
        });

        // apply matchHeight to each group
        $.each(groups, function() {
            this.matchHeight(true);
        });
    };

    /*
    *  matchHeight._update
    *  updates matchHeight on all current groups with their correct options
    */

    var _update = function(event) {
        if (matchHeight._beforeUpdate) {
            matchHeight._beforeUpdate(event, matchHeight._groups);
        }

        $.each(matchHeight._groups, function() {
            matchHeight._apply(this.elements, this.options);
        });

        if (matchHeight._afterUpdate) {
            matchHeight._afterUpdate(event, matchHeight._groups);
        }
    };

    matchHeight._update = function(throttle, event) {
        // prevent update if fired from a resize event
        // where the viewport width hasn't actually changed
        // fixes an event looping bug in IE8
        if (event && event.type === 'resize') {
            var windowWidth = $(window).width();
            if (windowWidth === _previousResizeWidth) {
                return;
            }
            _previousResizeWidth = windowWidth;
        }

        // throttle updates
        if (!throttle) {
            _update(event);
        } else if (_updateTimeout === -1) {
            _updateTimeout = setTimeout(function() {
                _update(event);
                _updateTimeout = -1;
            }, matchHeight._throttle);
        }
    };

    /*
    *  bind events
    */

    // apply on DOM ready event
    $(matchHeight._applyDataApi);

    // use on or bind where supported
    var on = $.fn.on ? 'on' : 'bind';

    // update heights on load and resize events
    $(window)[on]('load', function(event) {
        matchHeight._update(false, event);
    });

    // throttled update heights on resize events
    $(window)[on]('resize orientationchange', function(event) {
        matchHeight._update(true, event);
    });

});

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
        $("#" + targetElement.id).addClass('selected drag');
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
}
;
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
}
;
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
/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
function getAdjustedIndex(value) {
    switch ($('.board').attr('orientation').toLowerCase()) {
        case "black":
            return 7 - value;
        case "white":
            return value;
    }
}
function pieceClick(row, col) {
    $('.selected').removeClass('selected');
    $("#piece" + row + col).addClass('selected');
    $('.selected').closest('rect').css('stroke-width', '1');
}
function boardClick(row, col) {
    if ($('.selected').length === 0) {
        return;
    }
    var rowCol = $('.selected').attr('id').replace('piece', '');
    var startRow = getAdjustedIndex(parseInt(rowCol[0]));
    var startCol = getAdjustedIndex(parseInt(rowCol[1]));
    var endRow = getAdjustedIndex(row);
    var endCol = getAdjustedIndex(col);
    if (startRow === endRow || startCol === endCol) {
        $('.selected').closest('g').removeAttr('transform');
        $('.selected').removeClass('drag');
        return;
    }
    var transformX = 6.25 * (startCol - endCol);
    var transformY = 6.25 * (startRow - endRow);
    $('.selected').closest('g')[0].setAttributeNS(null, 'transform', 'translate(' + transformX + ',' + transformY + ')');
    $.ajax("/Board/MovePiece", {
        data: {
            id: $('.board').attr('id'),
            start: {
                row: startRow,
                column: startCol
            },
            end: {
                row: endRow,
                column: endCol
            }
        },
        dataType: 'html',
        method: 'POST',
        error: function () {
            $('.selected').closest('g').removeAttr('transform');
            $('.selected').removeClass('drag');
        }
    });
}
function undo() {
    $.ajax("/Board/Undo", {
        data: {
            id: $('.board').attr('id')
        },
        dataType: 'html',
        method: 'POST'
    });
}
function resign() {
    $.ajax("/Board/Resign", {
        data: {
            id: $('.board').attr('id')
        },
        dataType: 'html',
        method: 'POST'
    });
}
function joinGame() {
    $.ajax("/Board/Join", {
        data: {
            id: $('.board').attr('id')
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.board')[0].outerHTML = data;
            Init();
        }
    });
}
function displayGame(moveID) {
    $.ajax("/Board/DisplayGame", {
        data: {
            moveID: moveID,
            orientation: $('.board').attr('orientation')
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.board')[0].outerHTML = data;
            Init();
        }
    });
}
function flip() {
    switch ($('.board').attr('orientation').toLowerCase()) {
        case "black":
            $('.board').attr('orientation', 'White');
            break;
        case "white":
            $('.board').attr('orientation', 'Black');
            break;
    }
    var move = $('.moves li input:checked');
    if (move.length === 0) {
        move = $('.moves li input:last');
    }
    $.ajax("/Board/Orientate", {
        data: {
            id: $('.board').attr('id'),
            moveID: move.length === 0 ? '' : move.attr('id'),
            orientation: $('.board').attr('orientation')
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.board')[0].outerHTML = data;
            Init();
        }
    });
}
function getCookie(name) {
    var cookies = document.cookie.split(';');
    for (var index in cookies) {
        var cookie = cookies[index];
        if (cookie.indexOf('=') === -1) {
            continue;
        }
        var keyValue = cookie.split('=');
        if (keyValue[0].trim() === name) {
            return keyValue[1].trim();
        }
    }
    return '';
}
var signalRConnection;
function connectToSignalR() {
    var httpConnection = new signalR.HttpConnection('/signalRHub');
    signalRConnection = new signalR.HubConnection(httpConnection);
    signalRConnection.on('UpdateBoard', function (id, blackBoard, whiteBoard) {
        if ($('.board').attr('id').toLowerCase() === id.toLowerCase()) {
            switch ($('.board').attr('orientation').toLowerCase()) {
                case "black":
                    $('.board')[0].outerHTML = blackBoard;
                    break;
                case "white":
                    $('.board')[0].outerHTML = whiteBoard;
                    break;
            }
            Init();
        }
    });
    signalRConnection.on('UpdateMoves', function (data) {
        $('.moves')[0].outerHTML = data;
    });
    signalRConnection.on('UpdateOpponentState', function (player, winStatus) {
        switch (winStatus) {
            case "WhiteWin":
                $('.player-to-move').html('White Won');
                $('.win-status').html('1 - 0');
                break;
            case "BlackWin":
                $('.player-to-move').html('Black Won');
                $('.win-status').html('0 - 1');
                break;
            case "Drawn":
                $('.player-to-move').html('Game Drawn');
                $('.win-status').html('½ - ½');
                break;
            case "InProgress":
                $('.player-to-move').html(player + "'s Turn");
                $('.win-status').html('*');
                break;
        }
    });
    signalRConnection.on('SetAttribute', function (controlID, attribute, value) {
        $("#" + controlID).attr(attribute, value);
    });
    signalRConnection.on('RemoveAttribute', function (controlID, attribute) {
        $("#" + controlID).removeAttr(attribute);
    });
    signalRConnection.on('AddClass', function (controlID, value) {
        $("#" + controlID).addClass(value);
    });
    signalRConnection.on('RemoveClass', function (controlID, value) {
        $("#" + controlID).removeClass(value);
    });
    signalRConnection.on('SetHtml', function (selector, value) {
        $("" + selector).html(value);
    });
    signalRConnection.start().then(function () {
        var playerID = getCookie('playerID');
        if (playerID === '') {
            signalRConnection.invoke('GetNewPlayerID').then(function (value) {
                document.cookie += (document.cookie.trim() === '' ? '' : ';') + "playerID=" + value + ";path=/";
                signalRConnection.invoke('MapPlayerConnection', value);
            });
        }
        else {
            signalRConnection.invoke('MapPlayerConnection', playerID);
        }
    });
}
connectToSignalR();
//# sourceMappingURL=site.js.map