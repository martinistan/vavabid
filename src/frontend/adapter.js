// Umutistan VakantieVeilingen Smart Adapter & DOM Engine
(() => {
  function getMoney(text) {
    if (!text) return -1;
    const clean = text.replace(/[€\s\u00a0.]/g, '').replace(',', '.');
    const m = clean.match(/\d+(?:\.\d+)?/);
    return m ? parseFloat(m[0]) : -1;
  }

  function parseSeconds(timerStr) {
    if (!timerStr) return -1;
    const clean = timerStr.trim();
    // 00:05:03 or 05:03 or 5s
    const m3 = clean.match(/(\d{1,2}):(\d{2}):(\d{2})/);
    if (m3) return parseInt(m3[1]) * 3600 + parseInt(m3[2]) * 60 + parseInt(m3[3]);
    const m2 = clean.match(/(\d{1,2}):(\d{2})/);
    if (m2) return parseInt(m2[1]) * 60 + parseInt(m2[2]);
    const m1 = clean.match(/(\d+)\s*(?:s|sn|sec)/i);
    if (m1) return parseInt(m1[1]);
    return -1;
  }

  window.__bieder = {
    getState: function() {
      try {
        // 1. Root & Card Search
        const root = document.querySelector('#bid-block, .bid-block, [data-testid*="bid-block"]') || document;

        // 2. Timer Search
        let timerText = '';
        const timerEl = root.querySelector('.auction__countdown-text, [class*="countdown-text"], [class*="countdown"], [class*="timer"]') ||
                        document.querySelector('.auction__countdown-text, [class*="countdown-text"]');
        if (timerEl) {
          timerText = timerEl.innerText.replace(/\s+/g, ' ').trim();
        } else {
          // Fallback regex search on body
          const allText = root.innerText || '';
          const matchTimer = allText.match(/\b\d{2}:\d{2}:\d{2}\b/) || allText.match(/\b\d{2}:\d{2}\b/);
          if (matchTimer) timerText = matchTimer[0];
        }
        const secondsLeft = parseSeconds(timerText);

        // 3. Current Bid Search
        let currentBid = -1;
        const bidEl = root.querySelector('.lot-fact__value--current-bid, [data-testid="current-bid"], .current-bid');
        if (bidEl) {
          currentBid = getMoney(bidEl.innerText);
        } else {
          // Look for 'Huidig bod' label
          const allElements = root.querySelectorAll('*');
          for (let i = 0; i < allElements.length; i++) {
            const el = allElements[i];
            if (el.children.length === 0 && (el.innerText || '').includes('Huidig bod')) {
              const parent = el.parentElement;
              if (parent) {
                const priceMatch = (parent.innerText || '').match(/€\s*(\d+(?:[.,]\d+)?)/);
                if (priceMatch) {
                  currentBid = getMoney(priceMatch[0]);
                  break;
                }
              }
            }
          }
        }

        // 4. Input & Button
        const input = root.querySelector('input[aria-label="Jouw bod"], input[placeholder*="bod"], input[name*="bid"], input[type="number"]');
        const button = root.querySelector('#bid-submit, button[type="submit"], [data-testid="bid-submit"]') ||
                       Array.from(root.querySelectorAll('button')).find(b => (b.innerText || '').includes('Plaats bod'));

        // 5. Suggestions & Next Bid
        const suggestions = Array.from(root.querySelectorAll('[id^="bid-suggested-"], .suggested-btn, button[class*="suggested"]'))
          .map(e => getMoney(e.innerText))
          .filter(n => n > currentBid);
        let minBid = suggestions.length > 0 ? Math.min(...suggestions) : (currentBid > 0 ? currentBid + 1 : -1);

        // 6. Win & Loss
        const isWinner = !!root.querySelector('.bidblock-win, [class*="winner"], [class*="gewonnen"]');
        const isLost = !!root.querySelector('.bidblock-loss, [class*="verloren"]');
        const ownHighest = !!root.querySelector('.auction__bid--current .auction__bid--user, [class*="highest-bidder"]');

        // 7. Login check
        const loggedIn = !!document.querySelector('a[aria-label="Mijn account"], a[href*="account"], a[href*="myauctions"], [class*="user-menu"]');

        // 8. One Click Bid
        const oneClickToggle = root.querySelector('.form-switch input[type="checkbox"], input[type="checkbox"]');
        const oneClick = !!(oneClickToggle && oneClickToggle.checked);

        // 9. Title
        const titleEl = document.querySelector('h1, [class*="lot-title"], .auction-title');
        const title = (titleEl ? titleEl.innerText : document.title) || 'İhale';

        // An auction is recognized if we have a timer OR current bid OR bid button
        const isAuction = (secondsLeft >= 0) || (currentBid >= 0) || !!button || !!input;

        return {
          IsAuction: isAuction,
          IsWinner: isWinner,
          IsLost: isLost,
          OwnHighest: ownHighest,
          LoggedIn: loggedIn,
          OneClick: oneClick,
          CurrentBid: currentBid > 0 ? currentBid : 0,
          MinimumBid: minBid > 0 ? minBid : (currentBid > 0 ? currentBid + 1 : 1),
          Seconds: secondsLeft,
          TimerText: timerText || (secondsLeft >= 0 ? secondsLeft + 'sn' : '—'),
          Title: title.replace(/\n/g, ' ').trim()
        };
      } catch (err) {
        return {
          IsAuction: false,
          Error: err.toString(),
          CurrentBid: 0,
          MinimumBid: 1,
          Seconds: -1,
          TimerText: '—',
          Title: document.title
        };
      }
    },

    placeBid: function(amount) {
      try {
        const root = document.querySelector('#bid-block, .bid-block') || document;
        const input = root.querySelector('input[aria-label="Jouw bod"], input[placeholder*="bod"], input[name*="bid"], input[type="number"]');
        const button = root.querySelector('#bid-submit, button[type="submit"]') ||
                       Array.from(root.querySelectorAll('button')).find(b => (b.innerText || '').includes('Plaats bod'));

        if (input && amount > 0) {
          input.value = amount.toString();
          input.dispatchEvent(new Event('input', { bubbles: true }));
          input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        if (button && !button.disabled) {
          button.click();
          return true;
        }
        return false;
      } catch (e) {
        console.error('Bidding failed', e);
        return false;
      }
    }
  };

  // Dynamic Runtime Dark Mode Purifier (Removes white boxes/containers dynamically)
  function purifyDarkMode() {
    try {
      const candidates = document.querySelectorAll('div, section, article, main, aside, [class*="content"], [class*="help"], [class*="faq"], [class*="service"], [class*="card"]');
      for (let i = 0; i < candidates.length; i++) {
        const el = candidates[i];
        if (el.id === 'umutistan-dark') continue;
        const bg = window.getComputedStyle(el).backgroundColor;
        if (bg && bg.startsWith('rgb')) {
          const m = bg.match(/\d+/g);
          if (m && m.length >= 3) {
            const r = parseInt(m[0]), g = parseInt(m[1]), b = parseInt(m[2]);
            if (r > 210 && g > 210 && b > 210) {
              el.style.setProperty('background-color', '#121214', 'important');
              el.style.setProperty('color', '#f4f4f5', 'important');
            }
          }
        }
      }
    } catch (e) {}
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
      purifyDarkMode();
      setInterval(purifyDarkMode, 1200);
    });
  } else {
    purifyDarkMode();
    setInterval(purifyDarkMode, 1200);
  }
})();
