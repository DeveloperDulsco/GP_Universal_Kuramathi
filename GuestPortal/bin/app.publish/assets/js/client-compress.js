
'use strict'

const compress = new Compress()
//const preview = document.getElementById('preview')
//const output = document.getElementById('output')

//const upload = document.getElementById('upload')

//upload.addEventListener('change', (evt) => {
//    const files = [...evt.target.files]
//    compress.compress(files, {
//        size: 4, // the max size in MB, defaults to 2MB
//        quality: 0.75, // the quality of the image, max is 1,
//        maxWidth: 1920, // the max width of the output image, defaults to 1920px
//        maxHeight: 1920, // the max height of the output image, defaults to 1920px
//        resize: true // defaults to true, set false if you do not want to resize the image width and height
//    }).then((images) => {
//        console.log(images)
//        const img = images[0]
//        // returns an array of compressed images
//        preview.src = `${img.prefix}${img.data}`
//        console.log(img)

//        const { endSizeInMb, initialSizeInMb, iterations, sizeReducedInPercent, elapsedTimeInSeconds, alt } = img

//        output.innerHTML = `<b>Start Size:</b> ${initialSizeInMb} MB <br/><b>End Size:</b> ${endSizeInMb} MB <br/><b>Compression Cycles:</b> ${iterations} <br/><b>Size Reduced:</b> ${sizeReducedInPercent} % <br/><b>File Name:</b> ${alt}`
//    })
//}, false)






//"use strict"

//const options = {
//    targetSize: 0.25,
//    quality: 0.55,
//    maxWidth: 1600,
//    maxHeight: 1600,
//    resize: true, 
//}

//const compress = new Compress(options)

////const preview = document.getElementById("preview")
////const upload = document.getElementById("upload")




////upload.addEventListener(
////    "change",
////    (evt) => {
////        const files = [...evt.target.files]
////        compress.compress(files).then((conversions) => {
////            const { photo, info } = conversions[0]

////            console.log({ photo, info })

////            // Create an object URL which points to the photo Blob data
////            const objectUrl = URL.createObjectURL(photo.data)

////            // Set the preview img src to the object URL and wait for it to load
////            Compress.loadImageElement(preview, objectUrl).then(() => {
////                // Revoke the object URL to free up memory
////                URL.revokeObjectURL(objectUrl)
////            })

////            displayObject(photo, 'output-photo')
////            displayObject(info, 'output-info')
////        })
////    },
////    false
////)