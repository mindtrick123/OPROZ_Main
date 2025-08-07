// Custom JavaScript for OPROZ SaaS Platform

// Initialize tooltips
document.addEventListener('DOMContentLoaded', function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// Initialize popovers
document.addEventListener('DOMContentLoaded', function () {
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});

// Auto-hide alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});

// Form validation enhancement
document.addEventListener('DOMContentLoaded', function () {
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });
});

// Loading button state
function setButtonLoading(button, loading = true) {
    if (loading) {
        button.classList.add('btn-loading');
        button.disabled = true;
    } else {
        button.classList.remove('btn-loading');
        button.disabled = false;
    }
}

// Smooth scrolling for anchor links
document.addEventListener('DOMContentLoaded', function () {
    const anchorLinks = document.querySelectorAll('a[href^="#"]');
    anchorLinks.forEach(function (link) {
        link.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                e.preventDefault();
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
});

// Copy to clipboard functionality
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        // Show success message
        showToast('Copied to clipboard!', 'success');
    }).catch(function (err) {
        console.error('Failed to copy: ', err);
        showToast('Failed to copy to clipboard', 'error');
    });
}

// Toast notification system
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toast-container') || createToastContainer();
    const toastId = 'toast-' + Date.now();
    
    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type === 'error' ? 'danger' : type}" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    toastContainer.insertAdjacentHTML('beforeend', toastHTML);
    
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement);
    toast.show();
    
    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function () {
        toastElement.remove();
    });
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    container.style.zIndex = '1055';
    document.body.appendChild(container);
    return container;
}

// Progress bar animation
function animateProgressBar(progressBar, targetValue) {
    let currentValue = 0;
    const increment = targetValue / 100;
    
    const timer = setInterval(function () {
        currentValue += increment;
        if (currentValue >= targetValue) {
            currentValue = targetValue;
            clearInterval(timer);
        }
        progressBar.style.width = currentValue + '%';
        progressBar.setAttribute('aria-valuenow', currentValue);
    }, 20);
}

// Lazy loading for images
document.addEventListener('DOMContentLoaded', function () {
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver(function (entries, observer) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    imageObserver.unobserve(img);
                }
            });
        });
        
        const lazyImages = document.querySelectorAll('img[data-src]');
        lazyImages.forEach(function (img) {
            imageObserver.observe(img);
        });
    }
});

// Back to top button
document.addEventListener('DOMContentLoaded', function () {
    const backToTopButton = document.createElement('button');
    backToTopButton.className = 'btn btn-primary position-fixed bottom-0 end-0 m-3 rounded-circle';
    backToTopButton.style.display = 'none';
    backToTopButton.style.zIndex = '1050';
    backToTopButton.innerHTML = '<i class="fas fa-arrow-up"></i>';
    backToTopButton.setAttribute('aria-label', 'Back to top');
    
    document.body.appendChild(backToTopButton);
    
    window.addEventListener('scroll', function () {
        if (window.pageYOffset > 300) {
            backToTopButton.style.display = 'block';
        } else {
            backToTopButton.style.display = 'none';
        }
    });
    
    backToTopButton.addEventListener('click', function () {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
});

// Export functions for global use
window.OPROZ = {
    setButtonLoading,
    copyToClipboard,
    showToast,
    animateProgressBar
};