mergeInto(LibraryManager.library, {

  Hello: function () {
    window.alert("Hello, world!");
  },

  ShowAdv: function() {
    if (!window.ysdk) {
      console.warn("⚠️ YSDK не инициализирован (ShowAdv)");
      return;
    }

    window.ysdk.adv.showFullscreenAdv({
      callbacks: {
        onOpen: function() {
          SendMessage("Adv", "OnOpen");
        },
        onClose: function(wasShown) {
          SendMessage("Adv", "OnClose");
        },
        onError: function(error) {
          console.warn("❌ Ошибка фуллскрин рекламы:", error);
          SendMessage("Adv", "OnError");
        },
        onOffline: function(error) {
          SendMessage("Adv", "OnOffline");
        }
      }
    });
  },

  ShowReward: function(ptr) {
    if (!window.ysdk) {
      console.warn("⚠️ YSDK не инициализирован (ShowReward)");
      return;
    }

    var rewardType = UTF8ToString(ptr); // Преобразуем указатель из C# в строку JS

    window.ysdk.adv.showRewardedVideo({
      callbacks: {
        onOpen: function() {
          SendMessage("Adv", "OnOpenReward");
        },
        onRewarded: function() {
          SendMessage("Adv", "OnRewarded", rewardType);
        },
        onClose: function() {
          SendMessage("Adv", "OnCloseReward");
        },
        onError: function(e) {
          console.warn("❌ Ошибка rewarded рекламы:", e);
          SendMessage("Adv", "OnErrorReward");
        }
      }
    });
  },

  GetLang: function(){
    if (!window.ysdk || !ysdk.environment) {
      console.warn("⚠️ Не удалось получить язык — ysdk не инициализирован");
      var fallback = "en";
      var bufSize = lengthBytesUTF8(fallback) + 1;
      var buf = _malloc(bufSize);
      stringToUTF8(fallback, buf, bufSize);
      return buf;
    }

    var lang = window.ysdk.environment.i18n.lang;
    var bufferSize = lengthBytesUTF8(lang) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(lang, buffer, bufferSize);
    return buffer;
  },

  GameReady: function () {
    if (window.ysdk && ysdk.features && ysdk.features.LoadingAPI) {
      ysdk.features.LoadingAPI.ready();
      console.log("✅ Game is ready — LoadingAPI.ready()");
    } else {
      console.warn("⚠️ YSDK или LoadingAPI не инициализированы");
    }
  },

  LB_SetScore: function(lbNamePtr, score) {
  if (!window.ysdk) {
    console.warn("⚠️ YSDK not initialized (LB_SetScore)");
    SendMessage("LeaderboardBridge", "OnSetScoreFailed", "ysdk_not_ready");
    return;
  }

  var lbName = UTF8ToString(lbNamePtr);

  window.ysdk.isAvailableMethod("leaderboards.setScore").then(function(ok) {
    if (!ok) {
      SendMessage("LeaderboardBridge", "OnSetScoreFailed", "setScore_not_available");
      return;
    }

    window.ysdk.leaderboards.setScore(lbName, score).then(function() {
      SendMessage("LeaderboardBridge", "OnSetScoreOk", "");
    }).catch(function(e) {
      SendMessage("LeaderboardBridge", "OnSetScoreFailed", String(e));
    });
  });
},


  SaveExtern: function(date){
    try {
      var dateString = UTF8ToString(date);
      var myobj = JSON.parse(dateString);

      if (!window.player) {
        console.warn("⚠️ Player не инициализирован, данные не сохранены");
        return;
      }

      window.player.setData(myobj).then(() => {
        console.log("💾 Прогресс успешно сохранён:", myobj);
      }).catch(err => {
        console.error("❌ Ошибка сохранения данных:", err);
      });
    } catch (e) {
      console.error("❌ Ошибка в SaveExtern:", e);
    }
  },

  LoadExtern: function(){
    if (!window.player) {
      console.warn("⚠️ Player не инициализирован (LoadExtern)");
      return;
    }

    window.player.getData().then(_date => {
      const myJSON = JSON.stringify(_date);
      SendMessage("PlayerProgress", "SetPlayerProgress", myJSON);
      console.log("📦 Прогресс загружен:", _date);
    }).catch(err => {
      console.error("❌ Ошибка загрузки данных:", err);
    });
  },
   // ----------------- IAP (Yandex Payments) -----------------

  IAP_GetCatalog: function () {
    if (!window.payments) {
      console.warn("⚠️ Payments не инициализированы (IAP_GetCatalog)");
      SendMessage("PaymentsBridge", "OnCatalogFail", "payments_not_ready");
      return;
    }

    window.payments.getCatalog()
      .then(function (catalog) {
        SendMessage("PaymentsBridge", "OnCatalogOk", JSON.stringify(catalog || []));
      })
      .catch(function (err) {
        console.warn("❌ getCatalog failed:", err);
        SendMessage("PaymentsBridge", "OnCatalogFail", String(err));
      });
  },

  IAP_Purchase: function (productIdPtr) {
  var productId = UTF8ToString(productIdPtr);

  if (!window.payments) {
    console.warn("⚠️ Payments не инициализированы (IAP_Purchase)");
    SendMessage("PaymentsBridge", "OnPurchaseFail", "payments_not_ready");
    return;
  }

  window.payments.purchase({ id: productId })
    .then(function (purchase) {
      console.log("✅ RAW purchase =", purchase);

      // Нормализуем под Unity (чтобы не зависеть от формата)
      var norm = {
        productId: null,
        purchaseToken: null,
        signature: null
      };

      if (purchase) {
        // id товара
        norm.productId =
          purchase.productID ||
          purchase.productId ||
          purchase.id ||
          (purchase.product && purchase.product.id) ||
          productId;

        // токен (для consume)
        norm.purchaseToken = purchase.purchaseToken || null;

        // подпись (если signed:true)
        norm.signature = purchase.signature || null;
      }

      SendMessage("PaymentsBridge", "OnPurchaseOk", JSON.stringify(norm));
    })
    .catch(function (err) {
      console.warn("❌ purchase failed:", err);
      SendMessage("PaymentsBridge", "OnPurchaseFail", String(err));
    });
},

  IAP_Consume: function (tokenPtr) {
    var token = UTF8ToString(tokenPtr);

    if (!window.payments) {
      console.warn("⚠️ Payments не инициализированы (IAP_Consume)");
      SendMessage("PaymentsBridge", "OnConsumeFail", "payments_not_ready");
      return;
    }

    window.payments.consumePurchase(token)
      .then(function () {
        SendMessage("PaymentsBridge", "OnConsumeOk", token);
      })
      .catch(function (err) {
        console.warn("❌ consumePurchase failed:", err);
        SendMessage("PaymentsBridge", "OnConsumeFail", String(err));
      });
  },

  IAP_GetPurchases: function () {
  if (!window.payments) {
    console.warn("⚠️ Payments не инициализированы (IAP_GetPurchases)");
    SendMessage("PaymentsBridge", "OnGetPurchasesFail", "payments_not_ready");
    return;
  }

  window.payments.getPurchases()
    .then(function (resp) {
      console.log("✅ RAW getPurchases =", resp);

      var items = [];

      // Частый вариант: массив покупок
      if (Array.isArray(resp)) {
        for (var i = 0; i < resp.length; i++) {
          var p = resp[i] || {};
          items.push({
            productId: p.productID || p.productId || p.id || (p.product && p.product.id) || null,
            purchaseToken: p.purchaseToken || null
          });
        }

        SendMessage("PaymentsBridge", "OnGetPurchasesOk", JSON.stringify({ items: items, signature: null }));
        return;
      }

      // Иногда приходит объект (например { purchases:[...] } или { signature:"..." })
      if (resp && Array.isArray(resp.purchases)) {
        for (var j = 0; j < resp.purchases.length; j++) {
          var pp = resp.purchases[j] || {};
          items.push({
            productId: pp.productID || pp.productId || pp.id || (pp.product && pp.product.id) || null,
            purchaseToken: pp.purchaseToken || null
          });
        }

        SendMessage("PaymentsBridge", "OnGetPurchasesOk", JSON.stringify({ items: items, signature: resp.signature || null }));
        return;
      }

      // Если signed:true и вернулась только signature — без сервера ты НЕ сможешь безопасно обработать покупки
      if (resp && resp.signature) {
        console.warn("⚠️ getPurchases вернул только signature (signed:true). Без сервера это не обработать нормально.");
        SendMessage("PaymentsBridge", "OnGetPurchasesFail", "signed_true_signature_only");
        return;
      }

      // Ничего не поняли — отправим пусто, но без краша
      SendMessage("PaymentsBridge", "OnGetPurchasesOk", JSON.stringify({ items: [], signature: null }));
    })
    .catch(function (err) {
      console.warn("❌ getPurchases failed:", err);
      SendMessage("PaymentsBridge", "OnGetPurchasesFail", String(err));
    });
},

});