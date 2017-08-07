var OpenTsvn;
if (!OpenTsvn) OpenTsvn = {};

(function(ctx) {
    "use strict";

    var badge_manager = new ctx.BadgeManager();
    var opener = new ctx.TortoiseSvnOpener();
    var context_menu = new ctx.ContextMenu(opener, badge_manager);
    context_menu.registerHandler();

    var onInstalledCallback = function() {
        context_menu.createContextMenu();

        var data_migrator = new ctx.DataMigrator();
        data_migrator.migrate();
    };

    // First, persistent is set to true.
    onInstalledCallback();

    new ctx.MessageReceiver([ opener, badge_manager ]);

    browser.browserAction.onClicked.addListener(function() {
        badge_manager.showWarning(false);
        // Currently, Edge does not support openOptionsPage.
        // browser.runtime.openOptionsPage();
        browser.tabs.create({ url: browser.runtime.getURL("html/options.html") });
    });
})(OpenTsvn);
