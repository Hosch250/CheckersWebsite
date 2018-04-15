/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>

declare var signalR: any;

function getAdjustedIndex(value) {
    switch ($('.board').attr('player').toLowerCase()) {
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
    
    $.ajax("Board/MovePiece",
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
    $.ajax("Board/Undo",
        {
            data: {
                id: $('.board').attr('id')
            },
            dataType: 'html',
            method: 'POST'
        });
}

function displayGame(moveID) {
    $.ajax("Board/DisplayGame",
        {
            data: {
                moveID: moveID
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

    connection.on('Update', function (id, html) {

        if ($('.board').attr('id').toLowerCase() === id.toLowerCase()) {
            $('.board')[0].outerHTML = html;
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

connectToBoard();
connectToMoveControl();
connectToOpponents();