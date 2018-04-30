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
/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
var Coord = /** @class */ (function () {
    function Coord(Row, Col) {
        this.Row = Row;
        this.Col = Col;
    }
    return Coord;
}());
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
        error: function (err) {
            if (err.status === 403) {
                $('.selected').css('grid-area', parseInt(rowCol[0]) + 1 + " / " + (parseInt(rowCol[1]) + 1) + " / auto / auto");
                $('.selected').removeClass('drag');
            }
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
            GameInit();
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
            GameInit();
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
            GameInit();
        }
    });
}
function GetFEN() {
    var board = $('.piece').map(function (index, el) {
        return {
            id: el.id,
            player: $(el).attr('player'),
            piece: $(el).attr('pieceType')
        };
    });
    $.ajax("/BoardEditor/GetFEN", {
        data: {
            variant: $('#variant-board-editor').val(),
            startingPlayer: $('#player-board-editor').val(),
            pieces: board.get()
        },
        dataType: 'text',
        method: 'POST',
        success: function (data) {
            $('#fen-board-editor').val(data);
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
function updateBoardEditor() {
    $.ajax("/BoardEditor/GetBoard", {
        data: {
            variant: $('#variant-board-editor').val(),
            position: $('#position-board-editor').val()
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.board')[0].outerHTML = data;
        }
    });
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
            GameInit();
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
                document.cookie = "playerID=" + value + ";path=/";
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