function toggleOrder(orderId) {
    let content = document.getElementById("order-detail-" + orderId);
    content.classList.toggle("open");
}

function formatPhoneNumber(value) {
    const digits = value.replace(/\D/g, "").slice(0, 10);

    if (digits.length === 0) {
        return "";
    }

    if (digits.length <= 3) {
        return `(${digits}`;
    }

    if (digits.length <= 6) {
        return `(${digits.slice(0, 3)}) - ${digits.slice(3)}`;
    }

    return `(${digits.slice(0, 3)}) - ${digits.slice(3, 6)} - ${digits.slice(6)}`;
}

document.addEventListener("DOMContentLoaded", () => {
    const phoneInputs = document.querySelectorAll(
        "input[name$='PhoneNumber'], input[name$='PhoneNum'], input[id$='PhoneNumber'], input[id$='PhoneNum']"
    );

    phoneInputs.forEach((input) => {
        input.placeholder = "(222) - 222 - 2222";
        input.setAttribute("inputmode", "numeric");
        input.setAttribute("autocomplete", "tel-national");

        input.value = formatPhoneNumber(input.value);

        input.addEventListener("input", () => {
            input.value = formatPhoneNumber(input.value);
        });
    });

    const navbar = document.querySelector(".pantreats-navbar");

    if (!navbar || typeof bootstrap === "undefined" || !bootstrap.Dropdown) {
        return;
    }

    const dropdownItems = Array.from(navbar.querySelectorAll(".nav-item.dropdown"));
    const standardNavItems = Array.from(navbar.querySelectorAll(".nav-item:not(.dropdown)"));
    const desktopMediaQuery = window.matchMedia("(min-width: 1400px)");

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
