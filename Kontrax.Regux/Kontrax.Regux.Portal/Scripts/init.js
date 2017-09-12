$(function () {

    $('input[type=file]').change(function () {
        var fileName = $(this).val().split("\\").pop();
        $('.filePlaceholder').val(fileName);
    });

});
