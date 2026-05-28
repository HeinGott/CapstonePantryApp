function toggleOrder(orderId) {
    let content = document.getElementById("order-detail-" + orderId);
    content.classList.toggle("open");
}