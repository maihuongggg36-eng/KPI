/**
 * Command Palette (Ctrl+K), Keyboard Shortcuts & Onboarding Tour System
 * Học viện Hành chính Quốc gia - NAPA
 */

(function () {
    let selectedIndex = -1;
    let currentResults = [];
    let searchDebounceTimer = null;

    document.addEventListener("DOMContentLoaded", function () {
        initKeyboardShortcuts();
        initCommandPaletteEvents();
        checkFirstTimeTour();
    });

    // 1. Lắng nghe phím tắt toàn hệ thống
    function initKeyboardShortcuts() {
        document.addEventListener("keydown", function (e) {
            // Không kích hoạt phím đơn khi đang gõ trong input/textarea
            const isTyping = e.target.matches("input, textarea, select, [contenteditable]");

            // 1. Ctrl+K hoặc Cmd+K: Mở Command Palette
            if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) {
                e.preventDefault();
                openCommandPalette();
                return;
            }

            // 2. Shift+? hoặc phím ? khi không gõ text: Mở Cheat sheet Phím tắt
            if (!isTyping && (e.key === "?" || (e.shiftKey && e.key === "/"))) {
                e.preventDefault();
                openShortcutsModal();
                return;
            }

            // 3. Phím Esc: Đóng modals nếu đang mở
            if (e.key === "Escape") {
                closeAllModals();
            }
        });

        // Nút mở Command Palette trên Topbar
        const openBtn = document.getElementById("openCommandPaletteBtn");
        if (openBtn) {
            openBtn.addEventListener("click", openCommandPalette);
        }

        // Nút mở Cheat sheet Phím tắt
        const shortcutsBtn = document.getElementById("btnOpenShortcutsModal");
        if (shortcutsBtn) {
            shortcutsBtn.addEventListener("click", openShortcutsModal);
        }
    }

    // 2. Mở / Đóng Command Palette
    function openCommandPalette() {
        const modal = document.getElementById("commandPaletteModal");
        if (!modal) return;

        if (window.bootstrap && window.bootstrap.Modal) {
            const bsModal = bootstrap.Modal.getOrCreateInstance(modal);
            bsModal.show();
        } else {
            modal.classList.add("show");
            modal.style.display = "block";
        }

        const input = document.getElementById("commandPaletteInput");
        if (input) {
            input.value = "";
            setTimeout(() => input.focus(), 150);
            executeSearch(""); // Load gợi ý mặc định
        }
    }

    function openShortcutsModal() {
        const modal = document.getElementById("keyboardShortcutsModal");
        if (!modal) return;
        if (window.bootstrap && window.bootstrap.Modal) {
            bootstrap.Modal.getOrCreateInstance(modal).show();
        } else {
            modal.classList.add("show");
            modal.style.display = "block";
        }
    }

    function closeAllModals() {
        const cpModal = document.getElementById("commandPaletteModal");
        if (cpModal && window.bootstrap) {
            bootstrap.Modal.getInstance(cpModal)?.hide();
        }
    }

    // 3. Sự kiện trong Command Palette Input & Điều hướng phím mũi tên
    function initCommandPaletteEvents() {
        const input = document.getElementById("commandPaletteInput");
        if (!input) return;

        input.addEventListener("input", function () {
            clearTimeout(searchDebounceTimer);
            searchDebounceTimer = setTimeout(() => {
                executeSearch(input.value.trim());
            }, 180);
        });

        input.addEventListener("keydown", function (e) {
            if (e.key === "ArrowDown") {
                e.preventDefault();
                moveSelection(1);
            } else if (e.key === "ArrowUp") {
                e.preventDefault();
                moveSelection(-1);
            } else if (e.key === "Enter") {
                e.preventDefault();
                activateSelected();
            }
        });
    }

    function moveSelection(step) {
        if (currentResults.length === 0) return;
        selectedIndex += step;
        if (selectedIndex >= currentResults.length) selectedIndex = 0;
        if (selectedIndex < 0) selectedIndex = currentResults.length - 1;

        updateSelectionHighlight();
    }

    function updateSelectionHighlight() {
        const items = document.querySelectorAll(".cmd-palette-item");
        items.forEach((item, idx) => {
            if (idx === selectedIndex) {
                item.classList.add("active", "bg-primary", "text-white");
                item.scrollIntoView({ block: "nearest", behavior: "smooth" });
            } else {
                item.classList.remove("active", "bg-primary", "text-white");
            }
        });
    }

    function activateSelected() {
        if (selectedIndex >= 0 && selectedIndex < currentResults.length) {
            const item = currentResults[selectedIndex];
            saveRecentSearch(item.title);
            closeAllModals();
            if (item.url && item.url !== "#") {
                window.location.href = item.url;
            }
        }
    }

    // 4. Gọi API tìm kiếm nhanh
    function executeSearch(query) {
        const listContainer = document.getElementById("commandPaletteResults");
        if (!listContainer) return;

        listContainer.innerHTML = `
            <div class="p-4 text-center text-muted">
                <div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div>
                <span class="small">Đang tra cứu dữ liệu toàn hệ thống...</span>
            </div>`;

        fetch(`/Search/QuickSearch?q=${encodeURIComponent(query)}`)
            .then(res => res.json())
            .then(res => {
                if (res.success && res.data) {
                    currentResults = res.data.items || [];
                    selectedIndex = currentResults.length > 0 ? 0 : -1;
                    renderSearchResults(currentResults, query);
                }
            })
            .catch(err => {
                listContainer.innerHTML = `<div class="p-3 text-danger small">Lỗi tìm kiếm: ${err.message}</div>`;
            });
    }

    // 5. Render danh sách kết quả phân nhóm
    function renderSearchResults(items, query) {
        const listContainer = document.getElementById("commandPaletteResults");
        if (!listContainer) return;

        if (!items || items.length === 0) {
            listContainer.innerHTML = `
                <div class="p-5 text-center text-muted">
                    <i class="bi bi-search fs-1 d-block mb-2 opacity-50"></i>
                    <div class="fw-semibold text-dark">Không tìm thấy kết quả phù hợp</div>
                    <div class="small">Thử tìm kiếm với tên cán bộ, mã chỉ tiêu KPI hoặc tên phòng ban</div>
                </div>`;
            return;
        }

        // Nhóm theo category
        const groups = {};
        items.forEach((item, index) => {
            if (!groups[item.category]) groups[item.category] = [];
            groups[item.category].push({ item, globalIndex: index });
        });

        let html = "";
        for (const [catName, catItems] of Object.entries(groups)) {
            html += `
                <div class="px-3 pt-3 pb-1 text-uppercase text-secondary fw-bold" style="font-size: 11px; letter-spacing: 0.5px;">
                    ${catName}
                </div>`;

            catItems.forEach(({ item, globalIndex }) => {
                const isActive = globalIndex === selectedIndex ? "active bg-primary text-white" : "";
                const badge = item.badgeText ? `<span class="badge ${item.badgeClass || 'bg-light text-dark'} ms-2 small">${escapeHtml(item.badgeText)}</span>` : "";

                html += `
                    <div class="cmd-palette-item px-3 py-2 rounded-3 mx-2 d-flex align-items-center justify-content-between mb-1 ${isActive}" 
                         style="cursor: pointer; transition: background 0.15s ease;"
                         data-index="${globalIndex}"
                         onclick="window.selectPaletteItem(${globalIndex})">
                        <div class="d-flex align-items-center gap-3 text-truncate">
                            <div class="cmd-item-icon fs-5">
                                <i class="bi ${item.icon}"></i>
                            </div>
                            <div class="text-truncate">
                                <div class="fw-semibold cmd-title">${highlightText(item.title, query)} ${badge}</div>
                                <div class="cmd-subtitle small text-muted text-truncate" style="font-size: 11px;">${escapeHtml(item.subtitle)}</div>
                            </div>
                        </div>
                        <i class="bi bi-arrow-return-left small opacity-50 ms-2"></i>
                    </div>`;
            });
        }

        listContainer.innerHTML = html;
        updateSelectionHighlight();
    }

    window.selectPaletteItem = function (index) {
        selectedIndex = index;
        activateSelected();
    };

    function highlightText(text, q) {
        if (!q || !text) return escapeHtml(text);
        const escapedQ = q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const regex = new RegExp(`(${escapedQ})`, "gi");
        return escapeHtml(text).replace(regex, "<span class='text-warning fw-bold'>$1</span>");
    }

    function escapeHtml(str) {
        if (!str) return "";
        return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }

    function saveRecentSearch(text) {
        try {
            let recent = JSON.parse(localStorage.getItem("kpi_recent_searches") || "[]");
            recent = recent.filter(t => t !== text);
            recent.unshift(text);
            if (recent.length > 5) recent = recent.slice(0, 5);
            localStorage.setItem("kpi_recent_searches", JSON.stringify(recent));
        } catch (e) { }
    }

    // 6. Onboarding Tour cho người dùng mới
    function checkFirstTimeTour() {
        const seen = localStorage.getItem("kpi_tour_completed");
        if (!seen) {
            // Tự động nhắc sau 2 giây khi mới vào lần đầu
            setTimeout(() => {
                const startTourBtn = document.getElementById("btnTriggerTour");
                if (startTourBtn) {
                    // Hiển thị nút hoặc mở tour nếu muốn
                }
            }, 2000);
        }
    }

    window.startOnboardingTour = function () {
        const steps = [
            {
                element: "#openCommandPaletteBtn",
                title: "1. Tìm Kiếm Nhanh (Ctrl+K)",
                content: "Nhấn tổ hợp phím Ctrl + K để tìm kiếm tức thì nhân viên, chỉ tiêu KPI, phòng ban và điều hướng trang chức năng."
            },
            {
                element: "#notifBellDropdown",
                title: "2. Thông Báo Thời Gian Thực (SignalR)",
                content: "Nhận thông báo tự động có âm thanh và popup toast khi có sự kiện: Giao KPI, Nộp tự chấm, Thẩm định, Phê duyệt hoặc Cảnh báo hạn ngạch."
            },
            {
                element: "#sidebarMenuContainer",
                title: "3. Hệ Thống 7 Phân Hệ KPI",
                content: "Thanh menu bên trái chia theo các nhóm nghiệp vụ chuẩn mực: Giao chỉ tiêu, Tự đánh giá, Nghị định 90, Lương 3P và Báo cáo Chuông Gauss."
            },
            {
                element: "#userProfileDropdown",
                title: "4. Hồ Sơ & Cài Đặt Cá Nhân",
                content: "Tùy biến Theme Sáng/Tối, tải ảnh đại diện, đổi mật khẩu và cài đặt các loại thông báo muốn nhận."
            }
        ];

        let currentStep = 0;

        function showStep(idx) {
            if (idx >= steps.length) {
                localStorage.setItem("kpi_tour_completed", "true");
                const existing = document.getElementById("tourOverlayCard");
                if (existing) existing.remove();
                if (window.showToast) window.showToast("Chào mừng!", "Bạn đã hoàn thành phần giới thiệu các tính năng chính.", "success");
                return;
            }

            const step = steps[idx];
            const target = document.querySelector(step.element);

            let old = document.getElementById("tourOverlayCard");
            if (old) old.remove();

            const card = document.createElement("div");
            card.id = "tourOverlayCard";
            card.className = "card-enterprise shadow-2xl p-4 position-fixed border border-primary";
            card.style.zIndex = "99999";
            card.style.width = "360px";
            card.style.bottom = "30px";
            card.style.right = "30px";
            card.style.animation = "fadeInScale 0.3s ease";

            card.innerHTML = `
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <span class="badge bg-primary-subtle text-primary fw-bold font-mono">BƯỚC ${idx + 1} / ${steps.length}</span>
                    <button type="button" class="btn-close btn-sm" onclick="document.getElementById('tourOverlayCard').remove(); localStorage.setItem('kpi_tour_completed', 'true');"></button>
                </div>
                <h6 class="fw-bold text-dark mb-1">${step.title}</h6>
                <p class="text-secondary small mb-3">${step.content}</p>
                <div class="d-flex justify-content-between align-items-center">
                    <button type="button" class="btn btn-link btn-xs text-muted text-decoration-none" onclick="document.getElementById('tourOverlayCard').remove(); localStorage.setItem('kpi_tour_completed', 'true');">Bỏ qua</button>
                    <button type="button" class="btn btn-primary btn-sm rounded-pill px-3" id="btnNextTourStep">
                        ${idx === steps.length - 1 ? 'Hoàn tất &rarr;' : 'Tiếp tục &rarr;'}
                    </button>
                </div>`;

            document.body.appendChild(card);

            document.getElementById("btnNextTourStep").onclick = function () {
                showStep(idx + 1);
            };

            if (target) {
                target.classList.add("tour-highlight");
                setTimeout(() => target.classList.remove("tour-highlight"), 3000);
            }
        }

        showStep(0);
    };
})();
