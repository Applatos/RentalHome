
document.addEventListener("DOMContentLoaded", function() {
    const specsGallery = document.getElementById('specs');
    const specsFolder = "/Sommerhus resourcer/images/specs/"
    console.log("swaggg" + specsFolder)

    const images = ['spec1.png', 'spec2.png', 'spec3.png', 'spec4.png', 'spec5.png', 'spec6.png', 'spec7.png', 
        'spec8.png', 'spec9.png', 'spec10.png', 'spec11.png'];

    images.forEach(image => {
        const specElement = document.createElement('img');
        specElement.src = specsFolder + image;
        specsGallery.appendChild(specElement);
    });
});


let slideIndex = 1;
showSlides(slideIndex);

function plusSlides(n) {
    showSlides(slideIndex += n);
}

function currentSlide(n) {
    showSlides(slideIndex = n);
}

function showSlides(n) {
    let i;
    let slides = document.getElementsByClassName("mySlides");
    if (n > slides.length) { slideIndex = 1 }
    if (n < 1) { slideIndex = slides.length }
    for (i = 0; i < slides.length; i++) {
        slides[i].style.display = "none";
    }
    slides[slideIndex - 1].style.display = "block";
}