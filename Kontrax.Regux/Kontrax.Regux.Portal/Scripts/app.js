var app = (function () {
    var basesUrl = $('#applicationBaseUrl').attr('href');

    var pleaseWaitDiv = $('<div class="modal fade" tabindex="-1" role="dialog"  aria-hidden="true" id="pleaseWaitDialog" data-backdrop="static" data-keyboard="false"> \
                                        <div class="modal-dialog"> \
                                            <div class="modal-content"> \
                                                <div class="modal-header text-center"> \
                                                    <h1></h1> \
                                                </div> \
                                                <div class="modal-body"> \
                                                    <div class="progress progress-striped active"> \
                                                        <div class="bar progress-bar progress-bar-primary full-width-imortant"> \
                                                        </div> \
                                                    </div> \
                                                </div> \
                                            </div> \
                                        </div> \
                                    </div>');

    var showPleaseWait = function (customMessage) {
        var message = customMessage || 'Моля, изчакайте...';
        if (message) {
            pleaseWaitDiv.find('.modal-header h1').text(message);
        }
        pleaseWaitDiv.modal();
    };

    var simpleModal = $('<div id="simplModal" class="modal fade" tabindex="-1" role="dialog"> \
                            <div class="modal-dialog modal-sm" role="document"> \
                                <div class="modal-content"> \
                                    <div class="modal-header"> \
                                        <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button> \
                                        <h6 class="modal-title" id="simpleModalTitle"></h6> \
                                    </div> \
                                    <div class="modal-body"> \
                                        <h4 id="simpleModalText"></h4> \
                                    </div> \
                                    <div class="modal-footer"> \
                                        <button type="button" class="btn btn-danger" data-dismiss="modal">Затвори</button> \
                                    </div> \
                                </div> \
                            </div> \
                        </div>');

    var showSimpleModal = function (title, text) {
        simpleModal.find('#simpleModalTitle').text(title);
        simpleModal.find('#simpleModalText').text(text);
        simpleModal.modal();
    };

    var showSimpleModalAndAction = function (title, text, action) {
        simpleModal.find('#simpleModalTitle').text(title);
        simpleModal.find('#simpleModalText').text(text);
        $('#simplModal').on('shown.bs.modal', action);
        simpleModal.modal();
    };

    var hidePleaseWait = function () {
        pleaseWaitDiv.modal('hide');
    };

    var showSuccessMessage = function (message) {
        var success = $('<div id="success" class="alert alert-success alert-dismissible"> \
                            <a id="closeSuccess" href="#" class="close">&times;</a> \
                            <div class="text-center"><strong class="messageHolder"></strong></div> \
                        </div> ');
        $('body').append(success);

        success.find('.messageHolder').text(message);
        success.show('slow');
        window.setTimeout(function () {
            success.hide('slow');
        }, 3000);
    };

    var showErrorMessage = function (message) {
        var error = $('<div id="error" class="alert alert-danger"> \
                           <a id="closeError" href="#" class="close">&times;</a> \
                           <div class="text-center"><strong class="messageHolder"></strong></div> \
                       </div>');
        $('body').append(error);

        error.find('.messageHolder').text(message);
        error.show('slow');
        window.setTimeout(function () {
            error.hide('slow');
        }, 3000);
    };

    var showDeleteModal = function (url) {
        // TODO: Make it to load only 1 time!!!
        var urlPartsLength = url.split('/').length;
        if (urlPartsLength == 4) {
            url = url.substr(url.indexOf('/') + 1, url.length);
        }
        $.get(basesUrl + 'templates/deleteModal.html', function (data) {
            $('body').append(data);
            $('#deleteConfirmed').data('url', url);
            $('#deleteModal').modal('show');
        });
    };

    var multplyFloats = function (a, b) {
        if (typeof a == "string") {
            a = a.replace(/ /g, '');
        }
        if (typeof b == "string") {
            b = b.replace(/ /g, '');
        }
        var atens = Math.pow(10, String(a).length - String(a).indexOf('.') - 1),
            btens = Math.pow(10, String(b).length - String(b).indexOf('.') - 1);
        var result = (a * atens) * (b * btens) / (atens * btens);
        if (isNaN(result)) {
            return '';
        }
        return result.toFixed(4);
    };

    $(document).on("click", "#deleteConfirmed", function (event) {
        var url = $('#deleteConfirmed').data('url');
        $.ajax({
            method: 'POST',
            url: url,
            contentType: 'json',
            success: function (data) {
                hideDeleteModal();
                app.showSuccessMessage('Записът е изтрит успешно');
                app.reloadMvcAjaxGrid();
            },
            error: function (err) {
                hideDeleteModal();
                app.showErrorMessage('Записа не може да бъде изтрит, защото е свързан с други данни');
            }
        });
    });

    var reloadMvcAjaxGrid = function () {
        $('.mvc-grid').mvcgrid({
            reload: true
        });
    };

    var hideDeleteModal = function () {
        $('#deleteModal').modal('hide');
    };

    var initTooltip = function (element) {
        $(element).tooltip();
    };

    var initAllTooltips = function () {
        $('[data-toggle="tooltip"]').tooltip();
    };

    var initLastTooltip = function () {
        $('[data-toggle="tooltip"]').last().tooltip();
    };

    var initLastDatePicker = function () {
        $('.datepicker').last().datetimepicker({
            locale: moment.locale('bg'),
            format: 'DD.MM.YYYY'
        });
        $('.datepicker').click(function (event) {
            event.preventDefault();
            $(this).data("DateTimePicker").show();
        });
    };

    var initDatePicker = function (element) {
        $(element).datetimepicker({
            locale: moment.locale('bg'),
            format: 'DD.MM.YYYY',
            widgetPositioning: {
                horizontal: 'left',
                vertical: 'bottom'
            }
        });
        $(element).click(function (event) {
            event.preventDefault();
            $(this).data("DateTimePicker").show();
        });
    };

    var initLastSelect2AjaxDynamic = function () {
        $('.select2AjaxDynamic').last().select2ajax();
    };

    var initSelect2AjaxDynamic = function (element) {
        $(element).select2ajax();
    };

    var initLastSelect2 = function () {
        $('select.select2').last().select2({
            'placeholder': '-- Изберете --',
            'width': '100%',
            'allowClear': true
        }).on('select2:unselecting', function (e) {
            $(this).trigger('change');
        });
    }

    var initLastSelect2Dynamic = function () {
        $('select.select2Dynamic').last().select2({
            'placeholder': '-- Изберете --',
            'width': '100%',
            'allowClear': true
        }).on('select2:unselecting', function (e) {
            $(this).trigger('change');
        });;
    };

    var initSelect2Dynamic = function (element) {
        $(element).select2({
            'placeholder': '-- Изберете --',
            'width': '100%',
            'allowClear': true
        });

        $(element).on('select2:unselecting', function (e) {
            $(this).trigger('change');
        });
    };

    var showLoader = function () {
        var $loader = $('#loader');
        $loader.show();
    };

    var hideLoader = function () {
        $('#loader').hide();
    };

    var removeSpaceSeparator = function (numberString) {
        if (numberString) {
            return numberString.replace(/ /g, '');
        }
    };

    var pad = function (num, size) {
        var s = num + "";
        while (s.length < size) s = "0" + s;
        return s;
    }

    var isNumericRange = function (range) {
        var isNumeric = false;

        var rangePart = range.split('-');
        if (isInt(rangePart[0])) {
            isNumeric = true;
        }

        return isNumeric;
    };

    var generateNumericRange = function (baseRange, index) {
        var rangePart = baseRange.split('-');

        if (rangePart.length < 2) {
            app.showSimpleModal('Невалидна серия', 'Серията трябва да е с формат: число - число или символи'); // TODO: show this in external request view model and return;
        }

        var firstPartLength = rangePart[0].length;
        var firstPart = parseInt(rangePart[0]);
        firstPart += index + 1;
        var leadingZeroes = '';

        if (firstPartLength > firstPart.toString().length) {
            var idx = firstPartLength - firstPart.toString().length;
            for (var i = 0; i < idx; i++) {
                leadingZeroes += '0';
            }
        }

        return leadingZeroes + firstPart + '-' + rangePart[1];
    };

    var generateLetterRange = function (lastRange) {
        var range = '';

        var rangeParts = lastRange.split('-');

        $.ajax({
            url: app.baseUrl + 'api/ExternalRequest/GetLetterRange/' + rangeParts[0],
            contentType: 'text/plain',
            async: false
        }).done(function (data) {
            range = data.range;
        }).fail(function (xhr) {
            console.log('Error: ' + xhr);
            //range = xhr.responseText;
        });

        return range + (rangeParts.length < 2 ? '' : '-' + rangeParts[1]);
    };

    var isInt = function (value) {
        return !isNaN(value) &&
            parseInt(Number(value)) == value &&
            !isNaN(parseInt(value, 10));
    };

    return {
        baseUrl: basesUrl,
        showPleaseWait: showPleaseWait,
        hidePleaseWait: hidePleaseWait,
        initTooltip: initTooltip,
        initAllTooltips: initAllTooltips,
        initLastTooltip: initLastTooltip,
        showDeleteModal: showDeleteModal,
        hideDeleteModal: hideDeleteModal,
        showSuccessMessage: showSuccessMessage,
        showErrorMessage: showErrorMessage,
        reloadMvcAjaxGrid: reloadMvcAjaxGrid,
        showLoader: showLoader,
        hideLoader: hideLoader,
        multplyFloats: multplyFloats,
        initLastDatePicker: initLastDatePicker,
        initDatePicker: initDatePicker,
        initLastSelect2AjaxDynamic: initLastSelect2AjaxDynamic,
        initSelect2AjaxDynamic: initSelect2AjaxDynamic,
        initLastSelect2Dynamic: initLastSelect2Dynamic,
        initSelect2Dynamic: initSelect2Dynamic,
        showSimpleModal: showSimpleModal,
        removeSpaceSeparator: removeSpaceSeparator,
        initLastSelect2: initLastSelect2,
        isNumericRange: isNumericRange,
        generateNumericRange: generateNumericRange,
        generateLetterRange: generateLetterRange,
        isInt: isInt,
        pad: pad
    }
})();