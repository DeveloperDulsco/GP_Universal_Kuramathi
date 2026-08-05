
function moveToNextTab(currentTab) {

    var currentTabIndex = $('.tab-pane').index($(currentTab).parents('div.tab-pane'));

    var nextTab = $(currentTab).parents('div.tab-pane').next();

    if (nextTab.length > 0) {
        $(currentTab).parents('div.tab-pane').removeClass('active');
        $(currentTab).parents('div.tab-pane').next().addClass('active');
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