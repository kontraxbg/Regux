$(function () {

    $('input[type=file]').change(function () {
        var fileName = $(this).val().split("\\").pop();
        $('.filePlaceholder').val(fileName);
    });

    $('.datetimepicker').datetimepicker({
        locale: 'bg',
        format: 'DD.MM.YYYY HH:mm'
    });

    $(document).ready(function () {
        $('.select2-search-select').select2({
            "theme": "bootstrap"
        });
    });
});
