/// <reference path="../Scripts/typings/jquery/jquery.d.ts"/>
function $pieceClick(row, col) {
    $('.selected').removeClass('selected');
    $("#piece" + row + col).addClass('selected');
}
function $boardClick(row, col) {
    if ($('.selected').length === 0) {
        return;
    }
    var rowCol = $('.selected').attr('id').replace('piece', '');
    var startRow = parseInt(rowCol[0]);
    var startCol = parseInt(rowCol[1]);
    $.ajax("Board/MovePiece", {
        data: {
            id: $('.board').attr('id'),
            start: {
                row: startRow,
                column: startCol
            },
            end: {
                row: row,
                column: col
            }
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.board').parent().html(data);
            $loadMoveHistory();
        },
        error: function (err) {
            console.log(err);
        }
    });
}
function $loadMoveHistory() {
    console.log($('.board').attr('id'));
    $.ajax("Board/MoveHistory", {
        data: {
            id: $('.board').attr('id')
        },
        dataType: 'html',
        method: 'POST',
        success: function (data) {
            $('.moves').html(data);
        },
        error: function (err) {
            console.log(err);
        }
    });
}
//# sourceMappingURL=site.js.map