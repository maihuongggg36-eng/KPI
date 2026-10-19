/**
 * =============================================================================
 * HỆ THỐNG QUẢN LÝ KPI - HỌC VIỆN HÀNH CHÍNH VÀ QUẢN TRỊ CÔNG
 * THEME MANAGER & CLIENT INTERACTION ENGINE
 * Quản lý: Dark/Light Mode, Collapsible Sidebar, Scroll Progress, Command Palette (Ctrl+K), Toast System
 * =============================================================================
 */

(function () {
    'use strict';

    // 1. Quản lý Giao diện Sáng / Tối (Theme Manager)
    const THEME_KEY = 'kpi-theme';
    const SIDEBAR_KEY = 'kpi-sidebar-collapsed';

    function getPreferredTheme() {
        const savedTheme = localStorage.getItem(THEME_KEY);
        if (savedTheme) {
            return savedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        let activeTheme = theme;
        if (theme === 'system') {
            activeTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }

        document.documentElement.setAttribute('data-theme', activeTheme);
        localStorage.setItem(THEME_KEY, theme);

        // Cập nhật icon trên nút chuyển đổi
        const themeIcon = document.getElementById('themeToggleIcon');
        if (themeIcon) {
            if (theme === 'dark') {
                themeIcon.className = 'bi bi-moon-stars-fill text-warning';
            } else if (theme === 'light') {
                themeIcon.className = 'bi bi-sun-fill text-warning';
            } else {
                themeIcon.className = 'bi bi-circle-half text-primary';
            }
        }
    }

    // Khởi tạo theme ngay khi load
    const currentTheme = getPreferredTheme();
    applyTheme(currentTheme);

    // Lắng nghe thay đổi hệ điều hành nếu đặt theme là system
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        if (localStorage.getItem(THEME_KEY) === 'system') {
            applyTheme('system');
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        // 1.1 Gắn sự kiện nút Theme Switcher
        const themeBtn = document.getElementById('themeToggleBtn');
        if (themeBtn) {
            themeBtn.addEventListener('click', function () {
                const current = localStorage.getItem(THEME_KEY) || 'light';
                let nextTheme = 'dark';
                if (current === 'light') nextTheme = 'dark';
                else if (current === 'dark') nextTheme = 'system';
                else nextTheme = 'light';

                applyTheme(nextTheme);
                window.Toast?.show('Chuyển đổi giao diện', `Đã chuyển sang chế độ: ${nextTheme === 'dark' ? 'Tối (Dark Mode)' : nextTheme === 'light' ? 'Sáng (Light Mode)' : 'Tự động theo hệ thống'}`, 'info');
            });
        }

        // 2. Quản lý Sidebar Thu gọn (Collapsible Sidebar)
        const sidebar = document.getElementById('appSidebar');
        const mainWrapper = document.getElementById('appMain');
        const sidebarToggleBtn = document.getElementById('sidebarToggleBtn');
        const mobileMenuBtn = document.getElementById('mobileMenuBtn');

        function setSidebarState(collapsed) {
            if (!sidebar || !mainWrapper) return;
            if (collapsed) {
                sidebar.classList.add('collapsed');
                mainWrapper.classList.add('sidebar-collapsed');
                localStorage.setItem(SIDEBAR_KEY, 'true');
            } else {
                sidebar.classList.remove('collapsed');
                mainWrapper.classList.remove('sidebar-collapsed');
                localStorage.setItem(SIDEBAR_KEY, 'false');
            }
        }

        // Khôi phục trạng thái sidebar
        if (localStorage.getItem(SIDEBAR_KEY) === 'true' && window.innerWidth >= 1280) {
            setSidebarState(true);
        }

        if (sidebarToggleBtn) {
            sidebarToggleBtn.addEventListener('click', function () {
                const isCollapsed = sidebar.classList.contains('collapsed');
                setSidebarState(!isCollapsed);
            });
        }

        if (mobileMenuBtn && sidebar) {
            mobileMenuBtn.addEventListener('click', function () {
                sidebar.classList.toggle('mobile-open');
            });
        }

        // Đóng mobile sidebar khi click ra ngoài
        document.addEventListener('click', function (e) {
            if (window.innerWidth < 768 && sidebar && sidebar.classList.contains('mobile-open')) {
                if (!sidebar.contains(e.target) && !mobileMenuBtn?.contains(e.target)) {
                    sidebar.classList.remove('mobile-open');
                }
            }
        });

        // 3. Scroll Progress Bar
        const progressBar = document.getElementById('scrollProgressBar');
        if (progressBar) {
            window.addEventListener('scroll', function () {
                const scrollTop = window.scrollY || document.documentElement.scrollTop;
                const docHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
                const scrollPercent = docHeight > 0 ? (scrollTop / docHeight) * 100 : 0;
                progressBar.style.width = scrollPercent + '%';
            });
        }

        // 4. Command Palette (Ctrl+K / Cmd+K)
        const commandBackdrop = document.getElementById('commandPaletteBackdrop');
        const commandInput = document.getElementById('commandInput');
        const openCommandBtn = document.getElementById('openCommandPaletteBtn');

        function openCommandPalette() {
            if (commandBackdrop) {
                commandBackdrop.style.display = 'flex';
                commandInput?.focus();
            }
        }

        function closeCommandPalette() {
            if (commandBackdrop) {
                commandBackdrop.style.display = 'none';
                if (commandInput) commandInput.value = '';
            }
        }

        if (openCommandBtn) {
            openCommandBtn.addEventListener('click', openCommandPalette);
        }

        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                if (commandBackdrop && commandBackdrop.style.display === 'flex') {
                    closeCommandPalette();
                } else {
                    openCommandPalette();
                }
            }
            if (e.key === 'Escape' && commandBackdrop && commandBackdrop.style.display === 'flex') {
                closeCommandPalette();
            }
        });

        if (commandBackdrop) {
            commandBackdrop.addEventListener('click', function (e) {
                if (e.target === commandBackdrop) {
                    closeCommandPalette();
                }
            });
        }

        // 5. Toast Notification System
        window.Toast = {
            show: function (title, message, type = 'info', duration = 5000) {
                let container = document.getElementById('toastContainer');
                if (!container) {
                    container = document.createElement('div');
                    container.id = 'toastContainer';
                    container.className = 'toast-container-custom';
                    document.body.appendChild(container);
                }

                const toast = document.createElement('div');
                toast.className = 'toast-item-custom';

                const iconMap = {
                    success: 'bi-check-circle-fill text-success',
                    warning: 'bi-exclamation-triangle-fill text-warning',
                    danger: 'bi-x-circle-fill text-danger',
                    info: 'bi-info-circle-fill text-info'
                };

                const icon = iconMap[type] || iconMap.info;

                toast.innerHTML = `
                    <i class="bi ${icon} fs-4 flex-shrink-0"></i>
                    <div class="flex-grow-1">
                        <div class="fw-bold small mb-1">${title}</div>
                        <div class="text-secondary small" style="line-height: 1.3;">${message}</div>
                    </div>
                    <button type="button" class="btn-close btn-sm flex-shrink-0 ms-2" aria-label="Close"></button>
                `;

                const closeBtn = toast.querySelector('.btn-close');
                closeBtn.addEventListener('click', () => {
                    toast.remove();
                });

                container.appendChild(toast);

                setTimeout(() => {
                    toast.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                    toast.style.opacity = '0';
                    toast.style.transform = 'translateX(100%)';
                    setTimeout(() => toast.remove(), 400);
                }, duration);
            }
        };
    });
})();
