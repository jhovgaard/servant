//$("form").submit(function () {
//    var form = $(this);
//    var newUrl = $("#servanturl").val();

//    if (originalServantUrl != newUrl && !repost) {
//        var inputs = $(this).serialize();
//        $.post(form.attr("action"), inputs, function (response) {
//            if (response.Success) {
//                $(this).find("input[type=submit]").attr("disabled", "disabled");
//                setTimeout(function () {
//                    window.location = response.Url;
//                }, 1000);
//            } else {
//                repost = true;
//                form.submit();
//            }
//        });

//        return false;
//    }
//});