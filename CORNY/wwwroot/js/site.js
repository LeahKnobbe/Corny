// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const cartToast = document.getElementById("cart-toast");
if (cartToast) {
    setTimeout(() => cartToast.classList.add("d-none"), 3000);
}
