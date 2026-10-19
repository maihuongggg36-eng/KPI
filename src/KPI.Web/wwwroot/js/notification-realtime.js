/**
 * KPI Enterprise Notification & SignalR Real-time System
 * Học viện Hành chính Quốc gia - NAPA
 */

(function () {
    let unreadCount = 0;
    let isSoundEnabled = localStorage.getItem("kpi_sound_enabled") !== "false"; // Default true
    let signalrConnection = null;

    document.addEventListener("DOMContentLoaded", function () {
        initSoundToggle();
        loadNotifications();
        initSignalR();
    });

    // 1. Web Audio API Beep Sound (Không cần file mp3 ngoài, nhẹ và chạy offline)
    function playNotificationSound() {
        if (!isSoundEnabled) return;
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return;
            const ctx = new AudioContext();

            // Chimes 2 nốt nhẹ nhàng thanh lịch: 587Hz (D5) -> 880Hz (A5)
            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();
            osc1.type = "sine";
            osc1.frequency.setValueAtTime(587.33, ctx.currentTime);
            osc1.frequency.exponentialRampToValueAtTime(880.00, ctx.currentTime + 0.15);

            gain1.gain.setValueAtTime(0.12, ctx.currentTime);
            gain1.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.35);

            osc1.connect(gain1);
            gain1.connect(ctx.destination);

            osc1.start();
            osc1.stop(ctx.currentTime + 0.35);
        } catch (e) {
            console.warn("AudioContext error:", e);
        }
    }

    // 2. Sound Toggle Button trên Header Dropdown
    function initSoundToggle() {
        const soundBtn = document.getElementById("btnToggleNotifSound");
        if (soundBtn) {
            updateSoundIcon(soundBtn);
            soundBtn.addEventListener("click", function (e) {
                e.stopPropagation();
                isSoundEnabled = !isSoundEnabled;
                localStorage.setItem("kpi_sound_enabled", isSoundEnabled);
                updateSoundIcon(soundBtn);
                if (isSoundEnabled) playNotificationSound();
            });
        }
    }

    function updateSoundIcon(btn) {
        if (isSoundEnabled) {
            btn.innerHTML = '<i class="bi bi-volume-up-fill text-primary"></i>';
            btn.title = "Âm thanh thông báo: Đang BẬT (Click để tắt)";
        } else {
            btn.innerHTML = '<i class="bi bi-volume-mute-fill text-secondary"></i>';
            btn.title = "Âm thanh thông báo: Đang TẮT (Click để bật)";
        }
    }

    // 3. Tải danh sách thông báo từ API
    function loadNotifications() {
        fetch("/Notification/GetRecent?limit=20")
            .then(res => res.json())
            .then(res => {
                if (res.success && res.data) {
                    unreadCount = res.data.unreadCount || 0;
                    updateBadge(unreadCount);
                    renderNotificationList(res.data.recentNotifications || []);
                }
            })
            .catch(err => console.error("Lỗi tải thông báo:", err));
    }

    // 4. Render danh sách thông báo vào Dropdown panel
    function renderNotificationList(items) {
        const container = document.getElementById("notifDropdownList");
        if (!container) return;

        if (!items || items.length === 0) {
            container.innerHTML = `
                <div class="p-4 text-center text-muted">
                    <i class="bi bi-bell-slash fs-2 d-block mb-2 opacity-50"></i>
                    <span class="small">Chưa có thông báo mới nào</span>
                </div>`;
            return;
        }

        let html = "";
        items.forEach(item => {
            const unreadClass = item.isRead ? "opacity-75" : "bg-primary-subtle bg-opacity-10 fw-semibold";
            const dotIndicator = item.isRead ? "" : '<span class="badge bg-primary rounded-circle p-1 ms-auto" style="width: 8px; height: 8px;"></span>';

            html += `
                <div class="dropdown-item p-3 border-bottom text-wrap small d-flex gap-2 align-items-start position-relative notif-item ${unreadClass}" 
                     data-id="${item.id}" 
                     data-url="${item.targetUrl || '#'}" 
                     onclick="window.handleNotificationClick(${item.id}, '${item.targetUrl || ''}', event)"
                     style="cursor: pointer; transition: all 0.2s ease;">
                    <div class="mt-1 fs-5">
                        <i class="bi ${item.iconClass || 'bi-info-circle-fill text-primary'}"></i>
                    </div>
                    <div class="flex-grow-1 pe-2">
                        <div class="d-flex align-items-center justify-content-between mb-1">
                            <span class="badge ${item.badgeClass || 'bg-primary text-white'} rounded-pill" style="font-size: 10px;">
                                ${getTypeLabel(item.type)}
                            </span>
                            <span class="text-muted" style="font-size: 11px;">
                                <i class="bi bi-clock me-1"></i>${item.timeAgo}
                            </span>
                        </div>
                        <div class="fw-bold text-dark text-truncate" style="max-width: 260px;">${escapeHtml(item.title)}</div>
                        <div class="text-secondary small mt-1" style="line-height: 1.4;">${escapeHtml(item.content)}</div>
                    </div>
                    ${dotIndicator}
                </div>`;
        });

        container.innerHTML = html;
    }

    function getTypeLabel(type) {
        switch (type) {
            case 5: case "Urgent": return "Khẩn cấp";
            case 4: case "Error": return "Báo lỗi";
            case 3: case "Success": return "Thành công";
            case 2: case "Warning": return "Cảnh báo";
            default: return "Thông tin";
        }
    }

    // 5. Cập nhật Badge trên Topbar
    function updateBadge(count) {
        const badgeCount = document.getElementById("notifBadgeCount");
        const badgeDot = document.getElementById("notifBadgeDot");

        if (badgeCount) {
            badgeCount.textContent = count > 99 ? "99+" : count;
            badgeCount.style.display = count > 0 ? "inline-block" : "none";
        }
        if (badgeDot) {
            badgeDot.style.display = count > 0 ? "block" : "none";
        }

        const unreadLabel = document.getElementById("notifUnreadHeaderLabel");
        if (unreadLabel) {
            unreadLabel.textContent = count > 0 ? `${count} chưa đọc` : "Đã đọc hết";
        }
    }

    // 6. Xử lý click thông báo
    window.handleNotificationClick = function (id, url, event) {
        if (event) event.preventDefault();

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
        const formData = new FormData();
        formData.append("id", id);
        formData.append("__RequestVerificationToken", token);

        fetch("/Notification/MarkAsRead", {
            method: "POST",
            body: formData,
            headers: { "RequestVerificationToken": token }
        })
        .finally(() => {
            if (url && url !== "#" && url !== "") {
                window.location.href = url;
            } else {
                loadNotifications();
            }
        });
    };

    // 7. Đánh dấu tất cả đã đọc
    window.markAllNotificationsRead = function () {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
        const formData = new FormData();
        formData.append("__RequestVerificationToken", token);

        fetch("/Notification/MarkAllAsRead", {
            method: "POST",
            body: formData,
            headers: { "RequestVerificationToken": token }
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                unreadCount = 0;
                updateBadge(0);
                loadNotifications();
                showToast("Thông báo", "Đã đánh dấu tất cả thông báo là đã đọc.", "success");
            }
        })
        .catch(err => console.error("Lỗi:", err));
    };

    // 8. Hiển thị Toast Notification (Slide-in animation)
    window.showToast = function (title, content, type = "info", url = null) {
        const toastContainer = document.getElementById("kpiToastContainer");
        if (!toastContainer) return;

        const toastId = "toast_" + Date.now();
        const icon = type === "urgent" || type === "error"
            ? "bi-shield-fill-exclamation text-danger"
            : (type === "warning" ? "bi-exclamation-triangle-fill text-warning" : (type === "success" ? "bi-check-circle-fill text-success" : "bi-info-circle-fill text-primary"));

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-dark border-0 shadow-xl rounded-4 mb-2 toast-slide-in" role="alert" aria-live="assertive" aria-atomic="true" style="min-width: 300px; max-width: 380px;">
                <div class="d-flex p-3">
                    <div class="fs-4 me-3 mt-1">
                        <i class="bi ${icon}"></i>
                    </div>
                    <div class="toast-body flex-grow-1 p-0">
                        <div class="fw-bold text-white small">${escapeHtml(title)}</div>
                        <div class="text-white-50 small mt-1">${escapeHtml(content)}</div>
                        ${url ? `<div class="mt-2"><a href="${url}" class="btn btn-xs btn-primary rounded-pill px-3 text-white text-decoration-none">Xem ngay &rarr;</a></div>` : ""}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-1 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>`;

        toastContainer.insertAdjacentHTML("beforeend", toastHtml);
        const toastElem = document.getElementById(toastId);
        if (window.bootstrap && window.bootstrap.Toast) {
            const bsToast = new window.bootstrap.Toast(toastElem, { delay: 6000 });
            bsToast.show();
            toastElem.addEventListener("hidden.bs.toast", () => toastElem.remove());
        } else {
            setTimeout(() => toastElem.remove(), 6000);
        }
    };

    // 9. Khởi tạo kết nối SignalR Client
    function initSignalR() {
        if (typeof signalR === "undefined") {
            // Nếu chưa có script SignalR, tự động nạp CDN
            const script = document.createElement("script");
            script.src = "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js";
            script.onload = startSignalR;
            document.head.appendChild(script);
        } else {
            startSignalR();
        }
    }

    function startSignalR() {
        try {
            signalrConnection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/notification")
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            signalrConnection.on("ReceiveNotification", function (notif) {
                console.log("[SignalR Notification Received]", notif);

                // Phát âm thanh
                playNotificationSound();

                // Tăng badge
                unreadCount++;
                updateBadge(unreadCount);

                // Thêm vào danh sách
                loadNotifications();

                // Bật popup Toast
                const typeName = (notif.type === 5 || notif.type === "Urgent") ? "urgent" : ((notif.type === 2 || notif.type === "Warning") ? "warning" : ((notif.type === 3 || notif.type === "Success") ? "success" : "info"));
                window.showToast(notif.title, notif.content, typeName, notif.targetUrl);
            });

            signalrConnection.start()
                .then(() => console.log("SignalR NotificationHub connected."))
                .catch(err => console.warn("SignalR start error:", err));
        } catch (e) {
            console.warn("SignalR setup exception:", e);
        }
    }

    function escapeHtml(str) {
        if (!str) return "";
        return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }
})();
