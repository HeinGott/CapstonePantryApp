function toggleOrder(orderId) {
    let content = document.getElementById("order-detail-" + orderId);
    content.classList.toggle("open");
}

document.addEventListener("DOMContentLoaded", () => {
    const navbar = document.querySelector(".pantreats-navbar");

    if (!navbar || typeof bootstrap === "undefined" || !bootstrap.Dropdown) {
        return;
    }

    const dropdownItems = Array.from(navbar.querySelectorAll(".nav-item.dropdown"));
    const standardNavItems = Array.from(navbar.querySelectorAll(".nav-item:not(.dropdown)"));
    const desktopMediaQuery = window.matchMedia("(min-width: 768px)");

    const isDesktop = () => desktopMediaQuery.matches;

    const hideAllDropdowns = (exceptToggle = null) => {
        dropdownItems.forEach((item) => {
            const toggle = item.querySelector("[data-bs-toggle='dropdown']");

            if (!toggle || toggle === exceptToggle) {
                return;
            }

            bootstrap.Dropdown.getOrCreateInstance(toggle).hide();
        });
    };

    dropdownItems.forEach((item) => {
        const toggle = item.querySelector("[data-bs-toggle='dropdown']");

        if (!toggle) {
            return;
        }

        const showCurrentDropdown = () => {
            if (!isDesktop()) {
                return;
            }

            hideAllDropdowns(toggle);
            bootstrap.Dropdown.getOrCreateInstance(toggle).show();
        };

        item.addEventListener("mouseenter", showCurrentDropdown);
        item.addEventListener("focusin", showCurrentDropdown);
    });

    standardNavItems.forEach((item) => {
        const closeMenus = () => {
            if (isDesktop()) {
                hideAllDropdowns();
            }
        };

        item.addEventListener("mouseenter", closeMenus);
        item.addEventListener("focusin", closeMenus);
    });

    navbar.addEventListener("mouseleave", () => {
        if (isDesktop()) {
            hideAllDropdowns();
        }
    });

    document.addEventListener("click", (event) => {
        if (!navbar.contains(event.target)) {
            hideAllDropdowns();
        }
    });

    desktopMediaQuery.addEventListener("change", () => {
        if (!isDesktop()) {
            hideAllDropdowns();
        }
    });
});
