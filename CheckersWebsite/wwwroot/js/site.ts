/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>

declare var signalR: any;

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
    $(`#piece${row}${col}`).addClass('selected');

    $('.selected').closest('rect').css('stroke-width', '1');
}

function boardClick(row, col) {
    if ($('.selected').length === 0) {
        return;
    }

    var rowCol = $('.selected').attr('id').replace('piece', '');
    var startRow = getAdjustedIndex(parseInt(rowCol[0]));
    var startCol = getAdjustedIndex(parseInt(rowCol[1]));
    
    $.ajax("/Board/MovePiece",
        {
            data: {
                id: $('.board').attr('id'),
                start: {
                    row: startRow,
                    column: startCol
                },
                end: {
                    row: getAdjustedIndex(row),
                    column: getAdjustedIndex(col)
                }
            },
            dataType: 'html',
            method: 'POST'
        });
}

function undo() {
    $.ajax("/Board/Undo",
        {
            data: {
                id: $('.board').attr('id')
            },
            dataType: 'html',
            method: 'POST'
        });
}

function resign() {
    $.ajax("/Board/Resign",
        {
            data: {
                id: $('.board').attr('id')
            },
            dataType: 'html',
            method: 'POST'
        });
}

function joinGame() {
    $.ajax("/Board/Join",
        {
            data: {
                id: $('.board').attr('id')
            },
            dataType: 'html',
            method: 'POST'
        });
}

function displayGame(moveID) {
    $.ajax("/Board/DisplayGame",
        {
            data: {
                moveID: moveID,
                orientation: $('.board').attr('orientation')
            },
            dataType: 'html',
            method: 'POST',
            success(data) {
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

    var move = $('.moves li input:selected');
    if (move.length === 0) {
        move = $('.moves li input:last');
    }

    $.ajax("/Board/Orientate",
        {
            data: {
                id: $('.board').attr('id'),
                moveID: move.length === 0 ? '' : move.attr('id'),
                orientation: $('.board').attr('orientation')
            },
            dataType: 'html',
            method: 'POST',
            success(data) {
                $('.board')[0].outerHTML = data;
            }
        });
}

function connectToBoard() {
    let httpConnection = new signalR.HttpConnection('/boardHub');
    let connection = new signalR.HubConnection(httpConnection);

    connection.on('Update', function (id, blackBoard, whiteBoard) {

        if ($('.board').attr('id').toLowerCase() === id.toLowerCase()) {
            console.log($('.board').attr('orientation').toLowerCase());
            switch ($('.board').attr('orientation').toLowerCase()) {
                case "black":
                    $('.board')[0].outerHTML = blackBoard;
                    break;
                case "white":
                    $('.board')[0].outerHTML = whiteBoard;
                    break;
            }
        }
    });

    connection.start();
}

function connectToMoveControl() {
    let httpConnection = new signalR.HttpConnection('/movesHub');
    let connection = new signalR.HubConnection(httpConnection);

    connection.on('Update', function (data) {
        $('.moves')[0].outerHTML = data;
    });

    connection.start();
}

function connectToOpponents() {
    let httpConnection = new signalR.HttpConnection('/opponentsHub');
    let connection = new signalR.HubConnection(httpConnection);

    connection.on('Update', function (player, winStatus) {
        var players = ['black', 'white'];

        var otherPlayer = players.filter(item => item != player.toLowerCase())[0];
        $(`#${otherPlayer.toLowerCase()}-player-text`).removeClass('bold');

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
                $('.player-to-move').html(`${player}\'s Turn`);
                $('.win-status').html('*');
                $(`#${player.toLowerCase()}-player-text`).addClass('bold');
                break;
        }
    });

    connection.start();
}

function connectToControl() {
    let httpConnection = new signalR.HttpConnection('/controlHub');
    let connection = new signalR.HubConnection(httpConnection);

    connection.on('SetAttribute', function (controlID, attribute, value) {
        $(`#${controlID}`).attr(attribute, value);
    });

    connection.on('RemoveAttribute', function (controlID, attribute) {
        $(`#${controlID}`).removeAttr(attribute);
    });

    connection.on('AddClass', function (controlID, value) {
        $(`#${controlID}`).addClass(value);
    });

    connection.on('RemoveClass', function (controlID, value) {
        $(`#${controlID}`).removeClass(value);
    });

    connection.start();
}

connectToBoard();
connectToMoveControl();
connectToOpponents();
connectToControl();