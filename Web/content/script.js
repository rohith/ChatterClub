var ViewModels = (function (vm) {
    var userInfo = function (email, username) {
        var self = this;
        this.email = email;
        this.username = username;
        this.unread = ko.observable(0);
        this.chatSession = ko.observableArray();

        var getMessage = function (message, isReceived) {
            this.message = message;
            this.timeStamp = new Date().toLocaleTimeString() + ", " + new Date().toDateString();
            this.isReceived = isReceived;
        };

        this.messageReceived = function (message, incrementUnread) {
            self.chatSession.push(new getMessage(message, true));
            if (incrementUnread) {
                self.unread(self.unread() + 1);
            }
        };

        this.messageSent = function (message) {
            self.chatSession.push(new getMessage(message, false));
        };

        this.messagesViewed = function () {
            self.unread(0);
        };
    };

    vm.mainViewModel = (function () {
        var self = this;
        this.username = "";
        this.email = "";
        this.users = ko.observableArray();
        this.activeUser = ko.observable("");

        var getUser = function (email) {
            return ko.utils.arrayFirst(self.users(), function (user) {
                return user.email === email;
            });
        };

        this.userOnline = function (user) {
            var isNotSelf = false;
            if (!(getUser(user.email) || user.email === email)) {
                self.users.push(new userInfo(user.email, user.username));
                isNotSelf = true;
            }
            return isNotSelf;
        };

        this.activeUsers = function (others) {
            var otherUsers = $.map(others, function (user) {
                if (!(getUser(user.email) || user.email === email)) {
                    return new userInfo(user.email, user.username);
                }
            })
            self.users.push.apply(self.users, otherUsers);
        };

        this.userOffline = function (user) {
            self.users.remove(function (u) { return u.email === user.email; });
            self.activeUser("");
        };

        this.messageReceived = function (fromUserEmail, message, incrementUnread) {
            var user = getUser(fromUserEmail);
            if (user) {
                user.messageReceived(message, incrementUnread);
            }
            return user.username;
        };

        this.messageSent = function (toUserEmail, message) {
            var user = getUser(toUserEmail);
            if (user) {
                user.messageSent(message);
            }
        }

        this.messagesViewed = function (email) {
            var user = getUser(email);
            if (user) {
                user.messagesViewed();
            }
        };

        ko.applyBindings(this);
        return this;
    })();

    return vm;
})(ViewModels || {}),

DataManager = (function (dm) {
    var userInfoKey = "UserSessionDetails";

    var fillInUserDetails = function (email, username) {
        ViewModels.mainViewModel.username = username;
        ViewModels.mainViewModel.email = email;
    };

    dm.isBrowserSupported = function () {
        try {
            localStorage.setItem("CreativeColon", "CreativeColon");
            localStorage.removeItem("CreativeColon");
            return true;
        } catch (e) {
            return false;
        }
    };

    dm.store = function (item) {
        fillInUserDetails(item.email, item.username);
        localStorage.setItem(userInfoKey, JSON.stringify(item));
    };

    dm.fetch = function () {
        return $.parseJSON(localStorage.getItem(userInfoKey));
    };

    dm.erase = function () {
        fillInUserDetails("", "");
        localStorage.removeItem(userInfoKey);
    };

    var userDetail = dm.fetch();
    if (userDetail) {
        fillInUserDetails(userDetail.email, userDetail.username);
    }

    return dm;
})(DataManager || {}),

UX = (function (ux) {
    var isNotificationSupported = false;

    var setupUserForm = function () {
        $("div#user > form.form").form({
            fields: {
                username: ["empty", "maxLength[15]"],
                email: ["empty", "email"]
            },
            onSuccess: function (event, fields) {
                $(this).find("input").blur();
                ux.manageFormLoadingState(true);
                DataManager.store({ username: fields.username, email: fields.email });
                Hub.startHub(false);
                return false;
            }
        });
    };

    var setupVerifyForm = function () {
        $("div#verify > form.form").form({
            fields: {
                code: ["empty"]
            },
            onSuccess: function (event, fields) {
                $(this).find("input").blur();
                ux.manageFormLoadingState(true);
                Hub.chatter.server.verify($.trim(fields.code));
                return false;
            }
        });
    };

    var focusForm = function () {
        $("form.form:visible input:first").focus();
    };

    var clearForm = function () {
        $("form.form:visible input").val("");
    };

    var showApiNotification = function (message, email) {
        Notification.requestPermission(function () {
            var n = new Notification("Chatter Club", {
                tag: email,
                body: message
            });
            n.onclick = function (event) {
                event.preventDefault();
                window.focus();
                UX.setActiveUser(n.tag);
                this.close();
            };
            setTimeout(function () {
                n.close();
            }, 2500);
        });
    };

    var showLocalNotification = function (message) {
        $("#notification").text(message);
        $("#notification").transition({
            animation: "fade right",
            onComplete: function () {
                setTimeout(function () {
                    $("#notification").transition("fade right");
                }, 1000);
            }
        });
    };

    ux.manageFormLoadingState = function (isLoading) {
        var activeForm = $("form.form:visible");
        if (isLoading) {
            $(activeForm).addClass("loading");
        } else {
            $(activeForm).removeClass("loading");
        }
    };

    ux.sendUserToLogin = function () {
        DataManager.erase();
        location.reload(true);
    };

    ux.getUIReady = function () {
        if (ViewModels.mainViewModel.username !== "" && ViewModels.mainViewModel.email !== "") {
            Hub.startHub(true);
        } else {
            setupUserForm();
            setupVerifyForm();
            focusForm();
        }

        $(".sidebar.menu").sidebar("attach events", ".toggle-menu.item");
        if ("Notification" in window) {
            isNotificationSupported = true;
            Notification.requestPermission();
        }
    };

    ux.showVerifyScreen = function () {
        $("div#user").transition({
            animation: "fade right",
            onComplete: function () {
                $("div#verify").transition("fade left");
                focusForm();
            }
        });
    };

    ux.showChatScreen = function () {
        $("#entry").transition({
            animation: "fade down",
            onComplete: function () {
                $("#home").transition("fade up");
                $(".username-placeholder > span").text(ViewModels.mainViewModel.username);
                $(".username-placeholder").transition();
                $(".logout-switch").transition();
            }
        });
    };

    ux.displayBrowserNotSupported = function () {
        $("#continue").transition("fade down");
        $("#stop").transition("fade up");
    };

    ux.displayFormError = function (message) {
        var activeForm = $("form.form:visible"),
            errors = $("#errors");
        ux.manageFormLoadingState(false);
        clearForm();
        $(activeForm).find("input").blur();
        $(activeForm).find(".field").addClass("disabled");
        $(activeForm).find("button").addClass("disabled");
        $(errors).find("p").text(message);
        $(errors).transition({
            animation: "fade up",
            onComplete: function () {
                setTimeout(function () {
                    $(errors).transition("fade up");
                    $(activeForm).find(".field").removeClass("disabled");
                    $(activeForm).find("button").removeClass("disabled");
                    focusForm();
                }, 1500);
            }
        });
    };

    ux.setActiveUser = function (email) {
        ViewModels.mainViewModel.activeUser(email);
        $("div#chats").scrollTop($("div#chats").get(0).scrollHeight);
        ViewModels.mainViewModel.messagesViewed(email);
        $("#actions > div.input > input").focus();
    };

    ux.showNotification = function (message, email) {
        if (isNotificationSupported) {
            switch (Notification.permission) {
                case "denied":
                    break;
                default:
                    showApiNotification(message, email);
                    break;
            }
        } else {
            showLocalNotification(message);
        }
    };

    return ux;
})(UX || {}),

Hub = (function (hb) {
    var chatter = $.connection.chatterHub;
    var tryingToReconnect = false;

    chatter.client.proceedToVerifyScreen = function () {
        UX.showVerifyScreen();
    };

    chatter.client.authenticationCallback = function (isVerified) {
        if (isVerified) {
            UX.showChatScreen();
        } else {
            UX.displayFormError("The authentication code is not valid.");
        }
    };

    chatter.client.userOnline = function (user) {
        var isNotSelf = ViewModels.mainViewModel.userOnline(user);
        if (isNotSelf) {
            UX.showNotification("User Online: " + user.username, user.email);
        }
    };

    chatter.client.activeUsers = function (others) {
        ViewModels.mainViewModel.activeUsers(others);
    };

    chatter.client.userOffline = function (user) {
        ViewModels.mainViewModel.userOffline(user);
        UX.showNotification("User Offline: " + user.username, user.email);
    };

    chatter.client.showUserHistory = function (history) {
        // TODO: Get user history from server after implementing DB
    };

    chatter.client.receiveMessage = function (fromEmail, message) {
        var incrementUnread = true;
        if ($("#chats").find("div[data-id='" + fromEmail + "']:visible").length > 0) {
            incrementUnread = false;
        }
        var username = ViewModels.mainViewModel.messageReceived(fromEmail, message, incrementUnread);
        $("div#chats").scrollTop($("div#chats").get(0).scrollHeight);
        var formattedMessage = username + " says " + message.substring(0, 10) + ".....";
        UX.showNotification(formattedMessage, fromEmail);
    };

    chatter.client.sentMessage = function (toUser, message) {
        ViewModels.mainViewModel.messageSent(toUser, message);
        $("div#chats").scrollTop($("div#chats").get(0).scrollHeight);
    };

    chatter.client.showError = function () {
        UX.displayFormError(message);
    };

    chatter.client.sendUserToLoginScreen = function () {
        UX.sendUserToLogin();
    };

    hb.startHub = function (isRequestAutomated) {
        //$.connection.hub.logging = true;
        $.connection.hub.start().done(function () {
            chatter.server.getStarted(ViewModels.mainViewModel.email, ViewModels.mainViewModel.username, isRequestAutomated);

            $(".sidebar").on("click", "a.item", function () {
                var email = $(this).attr("data-id");
                UX.setActiveUser(email);
                $(".ui.sidebar").sidebar("toggle");
            });

            $("#users").on("click", "a.item", function () {
                var email = $(this).attr("data-id");
                UX.setActiveUser(email);
            });

            $("#actions > div.input > button").not(".disabled").click(function () {
                var inputBox = $(this).siblings("input");
                var message = $.trim(inputBox.val().replace(/</g, "&lt;").replace(/>/g, "&gt;"));
                inputBox.val("");
                inputBox.focus();
                var toUser = $("#users a.item.active").attr("data-id");
                if (message !== "") {
                    chatter.server.send(toUser, message);
                }
                $("div#chats").scrollTop($("div#chats").get(0).scrollHeight);
            });

            $("#actions > div.input > input").not(".disabled").keyup(function (e) {
                if (e.keyCode == 13) {
                    $(this).siblings("button").trigger("click");
                }
            });
        });
    };

    hb.stopHub = function () {
        DataManager.erase();
        ViewModels.mainViewModel = null;
        $.connection.hub.stop();
        location.reload(true);
    };

    $.connection.hub.connectionSlow(function () {
        console.log("Slow Connection");
    });

    $.connection.hub.reconnecting(function () {
        tryingToReconnect = true;
        console.log("Reconnecting");
    });

    $.connection.hub.reconnected(function () {
        tryingToReconnect = false;
        console.log("Reconnected");
    });

    $.connection.hub.disconnected(function () {
        if (tryingToReconnect) {
            console.log("Disconnected");
            setTimeout(function () {
                $.connection.hub.start();
            }, 3500);
        }
    });

    hb.chatter = chatter;
    return hb;
})(Hub || {});

$(function () {
    if (DataManager.isBrowserSupported()) {
        UX.getUIReady();
    } else {
        UX.displayBrowserNotSupported();
    }

    window.onbeforeunload = function () {
        console.log("closing");
    };

    window.onunload = function () {
        console.log("closed");
    };

    $(window).unload(function () {
        console.log("closed from jQuery");
    });
});