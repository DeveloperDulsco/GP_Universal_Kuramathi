const validateDocument1 = (document) => httpPost(BaseURL + '/api/portalservice/ValidateDocument', document).then(response => {
    return response;
});



$(".custom-file-input").on("change", function () {


    var fileName = $(this).val().split("\\").pop();
    var img = $(this).parent('div').find('img');


    var filextension = filename.split(".");
    var filext = "." + filextension.slice(-1)[0];
    var valid = [".jpg", ".png", ".jpeg"];

    if (valid.indexOf(filext.toLowerCase()) == -1) {

        // file format not supported


    }
    else {

        var reader = new FileReader();
        reader.onload = function (e) {
            $('.loader-screen').fadeIn(100);

            var imageBase64 = reader.result.replace(/^data:image.+;base64,/, '');
            var data = {
                extension: filextension.slice(-1)[0],
                imageBase64 :imageBase64
            };
            validateDocument(data).then(response => {

                if (response.result) {
                    //show success response
                }
                else {
                    //show error response
                }
                $('.loader-screen').fadeOut(800);

            }).catch(error => {
                $('.loader-screen').fadeOut(800);
            });


        };
        // read the image file as a data URL.
        reader.readAsDataURL(this.files[0]);
        $(this).siblings(".custom-file-label").addClass("selected").html(fileName);

    }

});