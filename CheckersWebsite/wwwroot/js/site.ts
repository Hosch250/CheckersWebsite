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

function $pieceClick(row, col) {
    $('.selected').removeClass('selected');
    $(`#piece${row}${col}`).addClass('selected');

    $('.selected').closest('rect').css('stroke-width', '1');
}

function $boardClick(row, col) {
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

function $undo() {
    $.ajax("Board/Undo",
        {
            data: {
                id: $('.board').attr('id')
            },
            dataType: 'html',
            method: 'POST',
            success(data) {
                $('.board').parent().html(data);
            }
        });
}