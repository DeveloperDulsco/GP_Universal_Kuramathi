
// Precheckin wizard steps (must match visible tab panes / menu)
var PRECHECKIN_STEPS = [
    { pageName: 'GuestDetails', tabId: 'guestDetails', tabIndex: 0 },
    { pageName: 'Policies', tabId: 'policies', tabIndex: 1 },
    { pageName: 'Document', tabId: 'document', tabIndex: 2 },
    { pageName: 'ThankYou', tabId: 'qrCode', tabIndex: 3 }
];

function getPrecheckinStepByTabId(tabId) {
    for (var i = 0; i < PRECHECKIN_STEPS.length; i++) {
        if (PRECHECKIN_STEPS[i].tabId === tabId) {
            return PRECHECKIN_STEPS[i];
        }
    }
    return null;
}

function getPrecheckinStepByIndex(tabIndex) {
    var idx = parseInt(tabIndex, 10);
    if (isNaN(idx)) {
        return null;
    }
    for (var i = 0; i < PRECHECKIN_STEPS.length; i++) {
        if (PRECHECKIN_STEPS[i].tabIndex === idx) {
            return PRECHECKIN_STEPS[i];
        }
    }
    return null;
}

function getPrecheckinStepByPageName(pageName) {
    if (!pageName) {
        return null;
    }
    var name = String(pageName).toLowerCase();
    for (var i = 0; i < PRECHECKIN_STEPS.length; i++) {
        if (PRECHECKIN_STEPS[i].pageName.toLowerCase() === name) {
            return PRECHECKIN_STEPS[i];
        }
    }
    return null;
}

/** Read CompletedTabIndex from new metadata shape (also tolerates old TabIndex). */
function metaCompletedTabIndex(row) {
    if (!row) {
        return -1;
    }
    var raw = (row.CompletedTabIndex != null) ? row.CompletedTabIndex
        : (row.completedTabIndex != null) ? row.completedTabIndex
        : (row.TabIndex != null) ? row.TabIndex
        : row.tabIndex;
    var idx = parseInt(raw, 10);
    return isNaN(idx) ? -1 : idx;
}

function normalizeMetaRows(responseData) {
    var rows = responseData;
    if (typeof rows === 'string') {
        try { rows = JSON.parse(rows); } catch (e) { rows = []; }
    }
    if (!$.isArray(rows)) {
        rows = [];
    }
    return rows;
}

function hasPrecheckinProgress(rows) {
    if (!rows || !rows.length) {
        return false;
    }
    for (var i = 0; i < rows.length; i++) {
        if (metaCompletedTabIndex(rows[i]) >= 0) {
            return true;
        }
    }
    return false;
}

function showPrecheckinSplash() {
    $('#mainDiv').hide();
    $('#divSplash').fadeIn(800);
}

function showPrecheckinWizard() {
    if (typeof beginRegistration === 'function') {
        beginRegistration();
    } else {
        $('#divSplash').hide();
        $('#mainDiv').fadeIn(800);
    }
}

function savePrecheckinProgress(completedTabIndex, allergies) {
    if (typeof ReservationNumber === 'undefined' || !ReservationNumber) {
        return;
    }
    if (typeof BaseURL === 'undefined' || !BaseURL) {
        return;
    }

    var payload = {
        ReservationNumber: String(ReservationNumber),
        CompletedTabIndex: String(completedTabIndex)
    };
    if (allergies != null && allergies !== undefined) {
        payload.Allergies = String(allergies);
    }

    $.ajax({
        url: BaseURL + '/api/portalservice/SaveReservationMetaData',
        type: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify(payload)
    });
}

function activatePrecheckinStep(step) {
    if (!step) {
        return;
    }

    try {
        var $target = $('#' + step.tabId);
        if (!$target.length) {
            // Document pane can be omitted when upload already complete
            if (step.tabId === 'document') {
                step = getPrecheckinStepByPageName('ThankYou') || step;
                $target = $('#' + step.tabId);
            }
            if (!$target.length) {
                return;
            }
        }

        $('.tab-pane').removeClass('active');
        $target.addClass('active');

        if (step.tabId === 'policies' && typeof initCanvas === 'function') {
            try { initCanvas(); } catch (e) { }
        }

        if (typeof setActiveTabs === 'function') {
            setActiveTabs(Math.max(step.tabIndex - 1, -1));
        }
        $("html, body").animate({ scrollTop: 0 }, "slow");
    } catch (e) {
        // Never block entry UI if tab activation fails
        console && console.error && console.error('activatePrecheckinStep', e);
    }
}

/**
 * New schema stores the highest completed tab index in CompletedTabIndex.
 * Resume opens the next incomplete step (completed + 1), capped at Thank You.
 */
function resolveResumeStep(rows) {
    if (!rows || !rows.length) {
        return PRECHECKIN_STEPS[0];
    }

    var latest = rows[0];
    var completedIdx = metaCompletedTabIndex(latest);
    if (completedIdx < 0) {
        return PRECHECKIN_STEPS[0];
    }

    if (completedIdx >= PRECHECKIN_STEPS.length - 1) {
        return PRECHECKIN_STEPS[PRECHECKIN_STEPS.length - 1];
    }

    return getPrecheckinStepByIndex(completedIdx + 1) || PRECHECKIN_STEPS[0];
}

/**
 * @param {boolean} handleEntry - when true, skip START splash if progress exists
 */
function resumePrecheckinProgress(handleEntry) {
    if (typeof ReservationNumber === 'undefined' || !ReservationNumber) {
        return;
    }
    if (typeof BaseURL === 'undefined' || !BaseURL) {
        return;
    }

    $.ajax({
        url: BaseURL + '/api/portalservice/FetchReservationMetaData',
        type: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        timeout: 8000,
        data: JSON.stringify({
            ReservationNumber: String(ReservationNumber)
        }),
        success: function (response) {
            var rows = [];
            if (response && (response.result === true || response.Result === true)) {
                var data = response.responseData != null ? response.responseData : response.ResponseData;
                if (data) {
                    rows = normalizeMetaRows(data);
                }
            }

            if (!hasPrecheckinProgress(rows)) {
                // Keep existing START splash (already shown by layout)
                return;
            }

            var step = resolveResumeStep(rows);
            if (handleEntry) {
                showPrecheckinWizard();
            }
            activatePrecheckinStep(step);
        },
        error: function () {
            // Keep START splash already shown by layout — never leave a blank page
        }
    });
}

function moveToNextTab(currentTab) {

    var $pane = $(currentTab).parents('div.tab-pane');
    var currentStep = getPrecheckinStepByTabId($pane.attr('id'));
    if (currentStep) {
        // Persist highest completed tab index for this reservation
        savePrecheckinProgress(currentStep.tabIndex);
    }

    var currentTabIndex = $('.tab-pane').index($pane);

    var nextTab = $pane.next();

    if (nextTab.length > 0) {
        $pane.removeClass('active');
        $pane.next().addClass('active');
        $("html, body").animate({ scrollTop: 0 }, "slow");
        setActiveTabs(currentTabIndex);
        if (+currentTabIndex == 0) {
            initCanvas();
        }
    }

    if (currentTabIndex == 2) {
        //if (IsDepositAvailable == "True") {
        //    $(nextTab).find('.btn_next').click();
        //}
    }
}

$(document).ready(function () {
    $('.tabs').on('click', function () {

        return;

        if ($(this).hasClass('paymentdone')) {
            return;
        }

        var target = $(this).attr('href');
        var tabIndex = $('.tabs').index($(this));
        if (+tabIndex == 0) {
            initCanvas();
        }

        if (+tabIndex == 1) {

            if (!validateTermsAndConditions()) {
                $('#messageContent').html("Please accept terms and conditions.");
                $('#exampleModal2').modal('show');
                return;
            }
            else {
                if (signaturePad.isEmpty()) {
                    $('#messageContent').html("Please Sign the registration card..");
                    $('#exampleModal2').modal('show');
                    return;
                }
            }
        }

        if ($("form[name='frm_guestdetails']").valid()) {

            $('.tabs').each(function (e) {

                var hTarget = $(this).attr('href');

                $(hTarget).removeClass('active');

            });

            $(target).addClass('active');

            $("html, body").animate({ scrollTop: 0 }, "slow");
        }

    });

    $('.moveNext').on('click', function (e) {
        e.preventDefault();

        var currentTabIndex = $('.tab-pane').index($(this).parents('div.tab-pane'));

        if ($("form[name='frm_guestdetails']").valid()) {
            moveToNextTab(this);
        }
        else {
            $('#messageContent').html("Please fill the missing details.");
            $('#exampleModal2').modal('show');
        }
    });

    $('.btn_cancel').on('click', function (e) {
        e.preventDefault();
        var prevTab = $(this).parents('div.tab-pane').prev();
        var currentTabIndex = $('.tab-pane').index($(this).parents('div.tab-pane'));
        if (prevTab.length > 0) {
            $(this).parents('div.tab-pane').removeClass('active');
            $(this).parents('div.tab-pane').prev().addClass('active');
            setActiveTabs(currentTabIndex - 2);
            $("html, body").animate({ scrollTop: 0 }, "slow");
            // Do not update tbReservationMetaData on Back — prior steps stay completed;
            // Back is only for reviewing entered details.
        }
    });
});


function validateTermsAndConditions() {
    return $('#acceptTermsAndCondition').is(':checked');
}


var initCanvas = function () {
    var wrapper = document.getElementById("signature-pad");
    var clearButton = wrapper.querySelector("[data-action=clear]");
    var changeColorButton = wrapper.querySelector("[data-action=change-color]");
    var canvas = wrapper.querySelector("canvas");

    signaturePad = new SignaturePad(canvas, {
        backgroundColor: 'rgb(255, 255, 255)'
    });

    clearButton.addEventListener("click", function (event) {
        signaturePad.clear();
    });
}

var setActiveTabs = function (currentTabIndex) {
    //loop and make tab title active till current tab
    var activeTabId = $('div.tab-pane.active').attr('id');
    if (activeTabId == "qrCode") {
        $('.tabs').each(function (i, e) {
        $(this).removeClass('active');
       
            $(this).addClass('active');
        });
    }
    else {
        $('.tabs').each(function (i, e) {


            $(this).removeClass('active');
            if (i <= +currentTabIndex + 1) {
                $(this).addClass('active');
            }
        });
    }
}

// Wait for the DOM to be ready
$(function () {

    $.validator.addMethod("time", function (value, element) {
        if (value) {
            return this.optional(element) || /^(([0-1]?[0-9])|([2][0-3])):([0-5]?[0-9])(:([0-5]?[0-9]))?$/i.test(value);
        }
        return false;
    }, "Please enter a valid time.");

    // Initialize form validation on the registration form.
    // It has the name attribute "registration"
    $("form[name='frm_guestdetails']").validate({
        // Specify validation rules
        rules: {
            // The key name on the left side is the name attribute
            // of an input field. Validation rules are defined
            // on the right side
            'Profiles[0].Phone': "required",
            'Profiles[0].AddressLine1': "required",
            'Profiles[0].City': "required",
            'Profiles[0].PostalCode': "required",
            'Profiles[0].StateID': {
                required: false,
            },
            'Profiles[0].Email': {
                required: true,
                email: true
            },
            'Profiles[0].CountryID': {
                required: true,
            },
            'ExpectedTimeofArrival': {
                time: true
            },

        },
        // Specify validation error messages
        messages: {
            'Profiles[0].Phone': "Please enter your phone number",
            'Profiles[0].AddressLine1': "Please enter your address",
            'Profiles[0].City': "Please enter your city",
            'Profiles[0].PostalCode': "Please enter Postal Code",
            'Profiles[0].StateID': "Please enter your state",
            'Profiles[0].Email': "Please enter a valid email address",
            'Profiles[0].CountryID': "Please select country",
            'ExpectedTimeofArrival': "Please enter expected time of arrival",

        },
        errorPlacement: function (error, element, label) {
            if (element.hasClass('selectpicker') && element.next('button').next('.dropdown-menu').length) {
                error.insertAfter(element.next('button').next('.dropdown-menu'));
            }
            else {
                error.insertAfter(element);

            }
        },
        // Make sure the form is submitted to the destination defined
        // in the "action" attribute of the form when valid
        submitHandler: function (form) {

            //form.submit();
        },
        invalidHandler: function (form, validator) {
            var errors = validator.numberOfInvalids();
            if (errors) {
                var el = validator.errorList[0].element;
                $('html, body').animate({ scrollTop: $(el).offset().top - 250 }, 'slow', function () {
                    validator.errorList[0].element.focus();
                });

            }
        }
    });


    $("form[name='frmDeclaration']").validate({
        errorPlacement: function (error, element, label) {
            if (element.hasClass('declarationRadio')) {

                if (element.parents('div.ans').length > 0) {
                    element.parents('div.ans').append(error);
                }
                else {
                    error.insertAfter(element);
                }
            }
            else {
                error.insertAfter(element);

            }
        },
        // Make sure the form is submitted to the destination defined
        // in the "action" attribute of the form when valid
        submitHandler: function (form) {

            //form.submit();
        },
        invalidHandler: function (form, validator) {
            var errors = validator.numberOfInvalids();
            if (errors) {
                var el = validator.errorList[0].element;
                $('html, body').animate({ scrollTop: $(el).offset().top - 250 }, 'slow', function () {
                    validator.errorList[0].element.focus();
                });

            }
        }
    });



});

function validateSetp() {




}
