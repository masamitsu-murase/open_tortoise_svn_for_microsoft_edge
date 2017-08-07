var OpenTsvn;
if (!OpenTsvn) OpenTsvn = {};

(function(ctx) {
    "use strict";

    const opener_sym = Symbol();
    const badge_manager_sym = Symbol();

    var ContextMenu = class ContextMenu {
        constructor(opener, badge_manager) {
            this[opener_sym] = opener;
            this[badge_manager_sym] = badge_manager;
        }

        registerHandler() {
            browser.contextMenus.onClicked.addListener(this.onContextMenu.bind(this));
        }

        createContextMenu() {
            browser.contextMenus.create({
                title: "Open TortoiseSVN",
                contexts: [ "page", "link" ],
                id: "parent"
            });
            [ "browser", "log", "blame", "open_in_browser" ].forEach(function(type) {
                var obj = {
                  title: browser.i18n.getMessage(`context_menu_${type}`),
                  contexts: (type === "open_in_browser" ? [ "link" ] : [ "page", "link" ]),
                  parentId: "parent",
                  id: type
                };
                browser.contextMenus.create(obj);
            });
        }

        onContextMenu(info, tab) {
            ctx.Misc.async(function*() {
                try {
                    var svn_url;
                    if (info.linkUrl) {
                        svn_url = new ctx.SvnUrl(info.linkUrl);
                    } else if (info.pageUrl) {
                        svn_url = new ctx.SvnUrl(info.pageUrl);
                    } else {
                        return;
                    }

                    switch (info.menuItemId) {
                      case "browser":
                        yield this.openRepobrowser(svn_url);
                        break;
                      case "log":
                        yield this.openLog(svn_url);
                        break;
                      case "blame":
                        yield this.openBlame(svn_url);
                        break;
                      case "open_in_browser":
                        yield this.openBrowser(svn_url, tab);
                        break;
                    }
                } catch (e) {
                    console.error(e);
                    this[badge_manager_sym].showWarning(true);
                }
            }, this);
        }

        openRepobrowser(svn_url) {
            // async
            return this[opener_sym].openRepobrowser(svn_url.url, (svn_url.p || svn_url.r));
        }

        openLog(svn_url) {
            // async
            return this[opener_sym].openLog(svn_url.url, (svn_url.p || svn_url.r), null);
        }

        openBlame(svn_url) {
            // async
            return this[opener_sym].openBlame(svn_url.url);
        }

        openBrowser(svn_url, tab) {
            return new Promise(function(resolve, reject) {
                browser.tabs.update(tab.id, { url: svn_url.url }, function() {
                    if (browser.runtime.lastError) {
                        reject(browser.runtime.lastError);
                        return;
                    }

                    resolve();
                });
            });
        }
    };

    ctx.ContextMenu = ContextMenu;
})(OpenTsvn);
