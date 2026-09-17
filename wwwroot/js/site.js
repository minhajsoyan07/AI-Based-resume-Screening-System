document.addEventListener('DOMContentLoaded', function () {
    // === GLOBAL DROPDOWN MANAGEMENT ===
    document.addEventListener('click', function (e) {
        // Match ANY clickable element inside a .nav-dropdown wrapper
        const dropdownTrigger = e.target.closest('.nav-dropdown > a, .nav-dropdown > button, .nav-dropdown > .btn');

        if (dropdownTrigger) {
            const parent = dropdownTrigger.closest('.nav-dropdown');
            const menu = parent ? parent.querySelector('.nav-dropdown-menu, .auth-dropdown, .user-dropdown-menu') : null;

            if (menu) {
                e.preventDefault();
                e.stopPropagation();

                const isExpanded = menu.classList.contains('show');

                // Close all other dropdowns first
                closeAllDropdowns();

                // Toggle the clicked one if it wasn't already open
                if (!isExpanded) {
                    menu.classList.add('show');
                    parent.classList.add('show');
                }
            }
        } else {
            // Clicked outside a trigger — close everything unless inside an open menu
            if (!e.target.closest('.nav-dropdown-menu, .auth-dropdown, .user-dropdown-menu')) {
                closeAllDropdowns();
            }
        }
    });

    function closeAllDropdowns() {
        document.querySelectorAll('.nav-dropdown-menu.show, .auth-dropdown.show, .user-dropdown-menu.show').forEach(function (menu) {
            menu.classList.remove('show');
            const parent = menu.closest('.nav-dropdown, .user-dropdown');
            if (parent) parent.classList.remove('show');
        });
    }

    // === ALERT AUTO-HIDE ===
    function setupAlerts() {
        document.querySelectorAll('.alert').forEach(function (a) {
            if (a.dataset.initialized) return;
            a.dataset.initialized = "true";

            var closeBtn = document.createElement('button');
            closeBtn.innerHTML = '&times;';
            closeBtn.className = 'alert-close-btn';
            closeBtn.setAttribute('aria-label', 'Close alert');
            closeBtn.onclick = function() {
                a.style.opacity = '0';
                setTimeout(function() { a.remove(); }, 300);
            };
            a.style.position = 'relative';
            a.appendChild(closeBtn);
            
            setTimeout(function () {
                if (a.parentNode) {
                    a.style.opacity = '0';
                    setTimeout(function () { a.remove(); }, 300);
                }
            }, 5000);
            a.style.transition = 'opacity 0.3s ease';
        });
    }
    setupAlerts();

    // === FORM VALIDATION ===
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            // If the form has a "no-validate" class, skip this
            if (form.classList.contains('no-validate')) return;

            var requiredInputs = form.querySelectorAll('[required], .required-field');
            var emptyFields = [];

            requiredInputs.forEach(function(input) {
                if (!input.value.trim()) {
                    var label = form.querySelector('label[for="' + input.id + '"]') ||
                                input.closest('.form-group, .mb-3, .mb-4')?.querySelector('.form-label');
                    var fieldName = label ? label.innerText.replace('*', '').trim() : (input.placeholder || 'Required field');
                    emptyFields.push(fieldName);
                    input.classList.add('is-invalid');
                } else {
                    input.classList.remove('is-invalid');
                }
            });

            if (emptyFields.length > 0) {
                e.preventDefault();
                if (window.Swal) {
                    Swal.fire({
                        title: 'Incomplete Information',
                        html: 'The following fields are required:<br><ul class="text-start mt-3 mb-0">' +
                              emptyFields.map(function(f) { return '<li>' + f + '</li>'; }).join('') + '</ul>',
                        icon: 'warning',
                        confirmButtonColor: '#007a5a',
                        confirmButtonText: "I'll fix it"
                    });
                } else {
                    alert('Please fill all required fields: ' + emptyFields.join(', '));
                }
                return false;
            }

            var btn = form.querySelector('button[type="submit"]');
            if (btn && !btn.disabled && !btn.classList.contains('no-spinner')) {
                btn.disabled = true;
                btn.dataset.originalText = btn.innerHTML;
                btn.innerHTML = '<span class="loading-spinner"></span> Processing...';
            }
        });
    });

    // === INITIALIZE OTHER MODULES ===
    if (typeof setupJobActions === 'function') setupJobActions();
    if (typeof setupSearchAutocomplete === 'function') setupSearchAutocomplete();
    if (typeof setupFilterDropdown === 'function') setupFilterDropdown();
});

// === GLOBAL UTILITIES ===
function confirmAction(message) {
    return confirm(message || 'Are you sure?');
}

function smartConfirm(event, title, text, confirmBtnText, isDanger) {
    event.preventDefault();
    var form = event.target.closest('form');
    var btn = event.target.closest('button[type="submit"]');

    if (window.Swal) {
        Swal.fire({
            title: title || 'Are you sure?',
            text: text || "This action cannot be undone.",
            icon: isDanger ? 'error' : 'warning',
            showCancelButton: true,
            confirmButtonColor: isDanger ? '#dc2626' : '#007a5a',
            cancelButtonColor: '#9CA3AF',
            confirmButtonText: confirmBtnText || 'Yes, proceed',
            cancelButtonText: 'Cancel',
            customClass: {
                popup: 'border-0',
                confirmButton: 'px-4 py-2 rounded-pill fw-semibold',
                cancelButton: 'px-4 py-2 rounded-pill border-0'
            }
        }).then(function (result) {
            if (result.isConfirmed) {
                if (form) {
                    var tempBtn = document.createElement('button');
                    tempBtn.type = 'submit';
                    tempBtn.style.display = 'none';
                    if (btn && btn.name) tempBtn.name = btn.name;
                    if (btn && btn.value) tempBtn.value = btn.value;
                    if (btn && btn.getAttribute('formaction')) tempBtn.setAttribute('formaction', btn.getAttribute('formaction'));

                    form.appendChild(tempBtn);
                    tempBtn.click();
                }
            }
        });
    } else {
        if (confirm(title + '\n' + text)) {
            if (form) form.submit();
        }
    }
}

function getScoreClass(score) {
    if (score >= 75) return 'high';
    if (score >= 50) return 'medium';
    return 'low';
}

function toggleSidebar() {
    var sb = document.querySelector('.sidebar, .app-sidebar');
    if (sb) sb.classList.toggle('open');
}

// === FILTER DROPDOWN MODULE ===
function setupFilterDropdown() {
    const filterToggle = document.getElementById('filterToggle');
    const filterMenu = document.getElementById('filterMenu');
    const clearFiltersBtn = document.getElementById('clearFilters');
    const applyFiltersBtn = document.getElementById('applyFilters');
    
    if (!filterToggle || !filterMenu) return;
    
    filterToggle.addEventListener('click', function(e) {
        e.stopPropagation();
        filterMenu.classList.toggle('active');
        filterToggle.classList.toggle('active');
    });
    
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function() {
            const inputs = ['jobTypeFilter', 'locationFilter', 'salaryMin', 'salaryMax', 'experienceFilter', 'industryFilter', 'dateFilter'];
            inputs.forEach(id => {
                const el = document.getElementById(id);
                if (el) el.value = '';
            });
            updateHiddenFields();
            filterMenu.classList.remove('active');
            filterToggle.classList.remove('active');
            const searchForm = document.querySelector('.search-row');
            if (searchForm) searchForm.submit();
        });
    }
    
    if (applyFiltersBtn) {
        applyFiltersBtn.addEventListener('click', function() {
            updateHiddenFields();
            filterMenu.classList.remove('active');
            filterToggle.classList.remove('active');
            const searchForm = document.querySelector('.search-row');
            if (searchForm) searchForm.submit();
        });
    }
    
    function updateHiddenFields() {
        const mappings = {
            'jobTypeFilter': 'jobTypeHidden',
            'locationFilter': 'locationHidden',
            'salaryMin': 'salaryMinHidden',
            'salaryMax': 'salaryMaxHidden',
            'experienceFilter': 'experienceHidden',
            'industryFilter': 'industryHidden',
            'dateFilter': 'datePostedHidden'
        };
        for (let src in mappings) {
            const srcEl = document.getElementById(src);
            const destEl = document.getElementById(mappings[src]);
            if (srcEl && destEl) destEl.value = srcEl.value;
        }
    }
    
    document.addEventListener('click', function(e) {
        if (!filterToggle.contains(e.target) && !filterMenu.contains(e.target)) {
            filterMenu.classList.remove('active');
            filterToggle.classList.remove('active');
        }
    });
}

// === JOB ACTIONS MODULE ===
function setupJobActions() {
    const bookmarkButtons = document.querySelectorAll('.job-action-btn.bookmark-btn');
    bookmarkButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const jobId = this.dataset.jobId;
            const isBookmarked = this.classList.contains('active');
            this.classList.toggle('active');
            
            fetch(`/Applicant/ToggleBookmark?jobId=${jobId}&bookmark=${!isBookmarked}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                }
            }).then(response => {
                if (!response.ok) this.classList.toggle('active');
            }).catch(() => this.classList.toggle('active'));
        });
    });

    const shareButtons = document.querySelectorAll('.job-action-btn.share-btn');
    shareButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const jobId = this.dataset.jobId;
            const shareData = {
                title: 'Check out this job opportunity',
                text: 'I found an interesting job you might like!',
                url: `${window.location.origin}/Applicant/JobDetails?jobId=${jobId}`
            };
            
            if (navigator.share) {
                navigator.share(shareData).catch(() => {});
            } else {
                navigator.clipboard.writeText(shareData.url).then(() => {
                    alert('Job link copied to clipboard!');
                });
            }
        });
    });
}

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// === SEARCH AUTOCOMPLETE MODULE ===
function setupSearchAutocomplete() {
    const searchInput = document.getElementById('searchInput');
    const autocompleteResults = document.getElementById('autocompleteResults');
    if (!searchInput || !autocompleteResults) return;
    
    let timeoutId = null;
    searchInput.addEventListener('input', function() {
        clearTimeout(timeoutId);
        const query = this.value.trim();
        if (query.length < 2) {
            autocompleteResults.innerHTML = '';
            autocompleteResults.style.display = 'none';
            return;
        }
        
        timeoutId = setTimeout(async () => {
            try {
                const response = await fetch(`/api/JobsApi/SearchSuggestions?term=${encodeURIComponent(query)}`);
                const suggestions = await response.json();
                if (suggestions.length === 0) {
                    autocompleteResults.innerHTML = '<div class="empty" style="padding:10px; color:#666;">No suggestions found</div>';
                } else {
                    autocompleteResults.innerHTML = suggestions.map(s => `<div class="suggestion-item" data-value="${s}" style="padding:10px; cursor:pointer; border-bottom:1px solid #eee;">${s}</div>`).join('');
                }
                autocompleteResults.style.display = 'block';
            } catch (error) {
                autocompleteResults.style.display = 'none';
            }
        }, 300);
    });
    
    autocompleteResults.addEventListener('click', function(e) {
        const item = e.target.closest('.suggestion-item');
        if (item) {
            searchInput.value = item.dataset.value;
            autocompleteResults.style.display = 'none';
            searchInput.closest('form').submit();
        }
    });
    
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !autocompleteResults.contains(e.target)) {
            autocompleteResults.style.display = 'none';
        }
    });
}

// === PASSWORD VISIBILITY TOGGLE ===
function togglePasswordVisibility(btn) {
    const parent = btn.parentElement;
    const input = parent.querySelector('input[type="password"], input[type="text"]');
    if (!input) return;
    const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
    input.setAttribute('type', type);
    
    if (type === 'text') {
        btn.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>';
    } else {
        btn.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>';
    }
}
