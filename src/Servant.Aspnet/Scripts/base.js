$(function () {
    $("abbr.timeago").timeago();

    $("#stop-site-form").submit(function () {
        var confirmed = confirm("Are you sure you want to completely stop this site?");
        if (!confirmed)
            return false;
    });
    
    $("#delete-site-form").submit(function () {
        var confirmed = confirm("Are you sure you want to delete this site? This action cannot be undone.");
        if (!confirmed)
            return false;
    });

    $("#bindings").on("change", "tr td input[name=https]", function () {
        var $certificate = $(this).parent().parent().parent().find("select");
        $certificate.toggle();
    });

    $("#bindings").on("keyup", "tr td input", function () {
        var value = $(this).val();
        if (value.length > 3) {
            var $certificate = $(this).parent().find("select");
            if (value.substring(0, 5).toLowerCase() == "https") {
                $certificate.show();
            } else {
                $certificate.hide();
            }
        }
    });
    
    $("#add-binding").click(function () {
        var bindings = $("#bindings");
        var newBinding = bindings.find("tbody:visible:last").clone();
        newBinding.find("select").hide();
        newBinding.find("td input").val("");
        newBinding.find("td").removeClass("error");
        newBinding.find("td span.help-block, td span.help-inline").remove();
        bindings.append(newBinding);
        bindings.find("tr td img").removeClass("hide");
        newBinding.find("td input").focus();

    });

    $("#bindings").on("click", "tr td img.remove-binding", function () {
        if ($("#bindings tbody:visible").length > 1) {
            $(this).parent().parent().parent().remove();
        }

        if ($("#bindings tr:visible").length == 1) {
            $("#bindings tr td img").addClass("hide");
        }

    });
    
    $(window).resize(function () {
        $("#sidemenu").height($(window).height());
    });

    $(function() {
        $("#sidemenu").height($(window).height());
        
        if (message.length) {
            var container = $("#message");
            container.find("div.text").html(message);
            container.fadeIn();
        }
    });
    
    // Form validation parser
    if(errors != null) {
        for (var error in errors) {
            error = errors[error];
            if (!error.IsGlobal) { // property
                var input;
                var firstIndexChar = error.PropertyName.indexOf("[");
                if (firstIndexChar > -1) {
                    var propertyName = error.PropertyName.substring(0, firstIndexChar);
                    var index = error.PropertyName.substring(firstIndexChar+1, error.PropertyName.indexOf("]"));
                    input = $($("input[name=" + propertyName + "]")[index]);
                } else {
                    input = $("input[name=" + error.PropertyName.toLowerCase() + "]");
                }

                var group = input.parents(".input-group");
                group.addClass("error");

                var helpSpan = group.find("span.help-inline, span.help-block");
                if (!helpSpan.length) {
                    var form = input.parents("form");
                    if (form.hasClass("form-horizontal"))
                        helpSpan = $('<span class="help-inline"/>');
                    else
                        helpSpan = $('<span class="help-block"/>');
                    
                    if(input.attr("type") == "checkbox") {
                        var label = input.parents("label");
                        label.after(helpSpan);
                    } else {
                        input.after(helpSpan);
                    }
                }
                helpSpan.text(error.Message);
                helpSpan.show();
            }
        }
    }
});
