//const deselectAll = function () {
//    $('.tick').each(function () {
//        $(this).hide();
//    })
//};


//$('.selectUpsell').on('click', function (e) {
//    e.preventDefault();
//    deselectAll();
//    $parent = $(this).parents('.upgrade');
//    $($parent).find('.tick').show();
//});

//deselectAll();

function enableDisableMembership(isChecked) {
    //alert(isChecked)
    //$('#membershipno').attr('disabled', isChecked)

    if (isChecked) {
        $('#MembershipNo').attr('readonly', true)
        $('#MembershipNo').val('')
    }
    else {
        $('#MembershipNo').attr('readonly', false)

    }
}


function enableDisableMembershipCheckbox() {

    if ($('#MembershipNo').val() != "") {
        $('#IsMembershipRequested').attr('disabled', true);
    }
    else {
        $('#IsMembershipRequested').attr('disabled', false);
    }

}



$('#country').on('change', function () {
    LoadStateByCountry($(this).val())
});



$(document).ready(function () {

    

});

