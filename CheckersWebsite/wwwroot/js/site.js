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
        $('.selected').closest('svg').removeAttr('transform');
        $('.selected').removeClass('drag');
        return;
    }
    var transformX = 6.25 * (startCol - endCol);
    var transformY = 6.25 * (startRow - endRow);
    $('.selected').closest('svg')[0].setAttributeNS(null, 'transform', 'translate(' + transformX + ',' + transformY + ')');
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
            $('.selected').closest('svg').removeAttr('transform');
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
            id: $('.board').attr('id'),
            connectionID: signalRConnection.connection.connectionId
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.board')[0].outerHTML = data;
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
    signalRConnection.start();
}
connectToSignalR();
//# sourceMappingURL=site.js.map