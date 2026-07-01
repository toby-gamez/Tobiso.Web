window.studyTimer = {
    requestNotificationPermission: async function () {
        if (!("Notification" in window)) return "denied";
        return await Notification.requestPermission();
    },
    notify: function (title, body) {
        if (Notification.permission === "granted") {
            new Notification(title, { body: body, icon: "/favicon.png" });
        }
    }
};
