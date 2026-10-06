// ============================================
// CamPro Security - site.js (client-facing)
// ============================================

function getCsrfToken() {
    var meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.getAttribute('content') : '';
}

function formatVnd(amount) {
    return Math.round(amount).toLocaleString('vi-VN') + ' đ';
}

function showToast(message, type) {
    type = type || 'success';
    var container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.style.position = 'fixed';
        container.style.top = '90px';
        container.style.right = '20px';
        container.style.zIndex = '2000';
        document.body.appendChild(container);
    }
    var toast = document.createElement('div');
    toast.className = 'alert alert-' + (type === 'success' ? 'success' : 'danger');
    toast.style.minWidth = '260px';
    toast.style.marginBottom = '10px';
    toast.style.boxShadow = '0 4px 16px rgba(0,0,0,0.15)';
    toast.innerText = message;
    container.appendChild(toast);
    setTimeout(function () { toast.remove(); }, 3000);
}

function updateCartBadge(count) {
    var badge = document.getElementById('cartBadge');
    if (badge) badge.innerText = count;
}

function refreshCartCount() {
    fetch('/Cart/GetCartCount').then(function (r) { return r.json(); }).then(function (data) {
        updateCartBadge(data.count || 0);
    }).catch(function () {});
}

function addToCart(productId, quantity) {
    quantity = quantity || 1;
    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
        body: 'productId=' + encodeURIComponent(productId) + '&quantity=' + encodeURIComponent(quantity)
    }).then(function (r) {
        return r.json();
    }).then(function (data) {
        if (!data) return;
        if (data.requireLogin) {
            showToast(data.message || 'Vui lòng đăng nhập trước khi thêm sản phẩm.', 'danger');
            setTimeout(function() {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            }, 1200);
            return;
        }
        if (data.success) {
            showToast(data.message, 'success');
            updateCartBadge(data.cartCount);
        } else {
            showToast(data.message || 'Có lỗi xảy ra.', 'danger');
        }
    }).catch(function () { showToast('Có lỗi xảy ra, vui lòng thử lại.', 'danger'); });
}

function toggleWishlist(productId, btn) {
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
        body: 'productId=' + encodeURIComponent(productId)
    }).then(function (r) {
        if (r.redirected) { window.location.href = r.url; return null; }
        return r.json();
    }).then(function (data) {
        if (!data) return;
        if (data.success) {
            btn.classList.toggle('active', data.added);
            var icon = btn.querySelector('i');
            if (icon) icon.className = data.added ? 'bi bi-heart-fill' : 'bi bi-heart';
            showToast(data.added ? 'Đã thêm vào yêu thích' : 'Đã bỏ khỏi yêu thích', 'success');
        }
    });
}

document.addEventListener('DOMContentLoaded', function () {
    refreshCartCount();

    // Add to cart (product cards + listing)
    document.body.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-add-cart');
        if (btn && !btn.disabled) {
            e.preventDefault();
            addToCart(btn.dataset.productId, 1);
        }

        var wishBtn = e.target.closest('.btn-wishlist, .btn-wishlist-lg');
        if (wishBtn) {
            e.preventDefault();
            toggleWishlist(wishBtn.dataset.productId, wishBtn);
        }

        // Cart page: qty buttons
        var minusBtn = e.target.closest('.cart-qty-minus');
        if (minusBtn) {
            var row = minusBtn.closest('tr');
            var input = row.querySelector('.cart-qty-input');
            input.value = Math.max(1, parseInt(input.value) - 1);
            input.dispatchEvent(new Event('change'));
        }
        var plusBtn = e.target.closest('.cart-qty-plus');
        if (plusBtn) {
            var row2 = plusBtn.closest('tr');
            var input2 = row2.querySelector('.cart-qty-input');
            var max = parseInt(input2.max) || 999;
            input2.value = Math.min(max, parseInt(input2.value) + 1);
            input2.dispatchEvent(new Event('change'));
        }
        var removeBtn = e.target.closest('.btn-remove-cart-item');
        if (removeBtn) {
            var row3 = removeBtn.closest('tr');
            var cartItemId = row3.dataset.cartItemId;
            fetch('/Cart/RemoveItem', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
                body: 'cartItemId=' + encodeURIComponent(cartItemId)
            }).then(function (r) { return r.json(); }).then(function (data) {
                if (data.success) {
                    row3.remove();
                    document.getElementById('cartSubTotal').innerText = formatVnd(data.subTotal);
                    document.getElementById('cartTotalQty').innerText = data.totalQuantity + ' sản phẩm';
                    updateCartBadge(data.totalQuantity);
                    if (data.isEmpty) window.location.reload();
                }
            });
        }
    });

    // Cart quantity change
    document.body.addEventListener('change', function (e) {
        if (e.target.classList.contains('cart-qty-input')) {
            var row = e.target.closest('tr');
            var cartItemId = row.dataset.cartItemId;
            var qty = parseInt(e.target.value) || 1;
            fetch('/Cart/UpdateQuantity', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getCsrfToken() },
                body: 'cartItemId=' + encodeURIComponent(cartItemId) + '&quantity=' + encodeURIComponent(qty)
            }).then(function (r) { return r.json(); }).then(function (data) {
                if (data.success) {
                    row.querySelector('.cart-line-total').innerText = formatVnd(data.lineTotal);
                    document.getElementById('cartSubTotal').innerText = formatVnd(data.subTotal);
                    document.getElementById('cartTotalQty').innerText = data.totalQuantity + ' sản phẩm';
                    updateCartBadge(data.totalQuantity);
                }
            });
        }
    });

    // Search suggest
    var searchInput = document.getElementById('searchInput');
    var suggestBox = document.getElementById('searchSuggestBox');
    if (searchInput && suggestBox) {
        var debounceTimer;
        searchInput.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            var q = searchInput.value.trim();
            if (q.length < 2) { suggestBox.classList.remove('show'); suggestBox.innerHTML = ''; return; }
            debounceTimer = setTimeout(function () {
                fetch('/Product/SearchSuggest?q=' + encodeURIComponent(q))
                    .then(function (r) { return r.json(); })
                    .then(function (items) {
                        if (!items.length) { suggestBox.classList.remove('show'); return; }
                        suggestBox.innerHTML = items.map(function (p) {
                            return '<a href="/san-pham/' + p.slug + '" class="suggest-item">' +
                                '<img src="' + p.image + '" /><span class="suggest-item-name">' + p.name + '</span>' +
                                '<span class="suggest-item-price">' + formatVnd(p.price) + '</span></a>';
                        }).join('');
                        suggestBox.classList.add('show');
                    });
            }, 300);
        });
        document.addEventListener('click', function (e) {
            if (!suggestBox.contains(e.target) && e.target !== searchInput) {
                suggestBox.classList.remove('show');
            }
        });
    }

    // Back to top
    var backToTop = document.getElementById('backToTop');
    if (backToTop) {
        window.addEventListener('scroll', function () {
            backToTop.classList.toggle('show', window.scrollY > 400);
        });
        backToTop.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    // Policy page nav highlight
    document.querySelectorAll('.policy-nav a').forEach(function (link) {
        link.addEventListener('click', function () {
            document.querySelectorAll('.policy-nav a').forEach(function (l) { l.classList.remove('active'); });
            this.classList.add('active');
        });
    });

    // ===== AI Chatbox Controller =====
    var aiToggleBtn = document.getElementById('aiChatToggleBtn');
    var aiChatBox = document.getElementById('aiChatBox');
    var aiCloseBtn = document.getElementById('aiChatCloseBtn');
    var aiForm = document.getElementById('aiChatForm');
    var aiInput = document.getElementById('aiChatInput');
    var aiMessages = document.getElementById('aiChatMessages');

    if (aiToggleBtn && aiChatBox) {
        aiToggleBtn.addEventListener('click', function () {
            aiChatBox.classList.toggle('d-none');
            if (!aiChatBox.classList.contains('d-none') && aiInput) {
                aiInput.focus();
                scrollChatToBottom();
            }
        });

        if (aiCloseBtn) {
            aiCloseBtn.addEventListener('click', function () {
                aiChatBox.classList.add('d-none');
            });
        }
    }

    function scrollChatToBottom() {
        if (aiMessages) {
            aiMessages.scrollTop = aiMessages.scrollHeight;
        }
    }

    function appendUserMessage(text) {
        if (!aiMessages) return;
        var msgDiv = document.createElement('div');
        msgDiv.className = 'ai-msg user';
        msgDiv.innerHTML = '<div class="msg-bubble">' + escapeHtml(text) + '</div>';
        aiMessages.appendChild(msgDiv);
        scrollChatToBottom();
    }

    function appendBotTypingIndicator() {
        if (!aiMessages) return null;
        var indicator = document.createElement('div');
        indicator.id = 'aiTypingIndicator';
        indicator.className = 'ai-msg bot';
        indicator.innerHTML = '<div class="ai-typing-indicator"><span></span><span></span><span></span></div>';
        aiMessages.appendChild(indicator);
        scrollChatToBottom();
        return indicator;
    }

    function removeBotTypingIndicator() {
        var el = document.getElementById('aiTypingIndicator');
        if (el) el.remove();
    }

    function appendBotMessage(data) {
        if (!aiMessages) return;
        removeBotTypingIndicator();
        var msgDiv = document.createElement('div');
        msgDiv.className = 'ai-msg bot';

        // Parse markdown basic: **bold**, list items, [link](url)
        var formatted = (data.reply || '')
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" class="text-primary fw-bold text-decoration-underline" target="_blank">$1</a>')
            .replace(/\n/g, '<br/>');

        var html = '<div class="msg-bubble"><p class="mb-1">' + formatted + '</p>';

        if (data.orderPlaced && data.orderCode) {
            html += '<div class="alert alert-success py-2 px-3 mt-2 mb-1 small">' +
                    '<i class="bi bi-check-circle-fill"></i> Đã tự động tạo đơn: <strong>' + data.orderCode + '</strong>' +
                    '<div class="mt-1"><a href="/Order/History" class="btn btn-sm btn-outline-success py-0 px-2">Xem lịch sử đơn</a></div>' +
                    '</div>';
        }

        if (data.suggestions && data.suggestions.length) {
            html += '<div class="mt-2 pt-2 border-top"><small class="text-muted fw-bold d-block mb-1">Gợi ý sản phẩm phù hợp:</small>';
            data.suggestions.forEach(function (s) {
                html += '<div class="ai-suggestion-card">' +
                        (s.imageUrl ? '<img src="' + s.imageUrl + '" alt="" />' : '') +
                        '<div class="flex-grow-1" style="min-width: 0;">' +
                        '<a href="/san-pham/' + s.slug + '" class="fw-bold text-dark text-truncate d-block" style="font-size:12px;">' + s.name + '</a>' +
                        '<span class="text-danger fw-bold small">' + formatVnd(s.price) + '</span>' +
                        '</div>' +
                        '<button type="button" class="btn btn-outline-primary btn-sm py-0 px-2 btn-ai-quick-order" data-name="' + s.name + '" title="Nhờ AI đặt nhanh">' +
                        '<i class="bi bi-cart-plus"></i> Đặt' +
                        '</button>' +
                        '</div>';
            });
            html += '</div>';
        }

        html += '</div>';
        msgDiv.innerHTML = html;
        aiMessages.appendChild(msgDiv);
        scrollChatToBottom();

        // Gắn sự kiện nút Đặt nhanh trên thẻ gợi ý
        msgDiv.querySelectorAll('.btn-ai-quick-order').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var pName = this.dataset.name;
                if (aiInput) {
                    aiInput.value = 'Đặt hộ tôi ' + pName;
                    aiForm.dispatchEvent(new Event('submit'));
                }
            });
        });
    }

    function escapeHtml(str) {
        return str.replace(/[&<>"']/g, function (m) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' }[m];
        });
    }

    if (aiForm && aiInput) {
        aiForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var text = aiInput.value.trim();
            if (!text) return;

            appendUserMessage(text);
            aiInput.value = '';
            appendBotTypingIndicator();

            fetch('/AiChat/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({ message: text })
            })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (data.requireLogin) {
                    removeBotTypingIndicator();
                    showToast(data.message || 'Vui lòng đăng nhập để sử dụng tính năng này!', 'warning');
                    setTimeout(function () {
                        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    }, 1200);
                    return;
                }
                appendBotMessage(data);
            })
            .catch(function (err) {
                removeBotTypingIndicator();
                appendBotMessage({ reply: 'Có lỗi xảy ra khi kết nối tới CamPro AI. Vui lòng thử lại sau giây lát!' });
            });
        });
    }
});

