$(function () {
    $("#stop-site-form").submit(function () {
        var confirmed = confirm("Are you sure you want to complete stop this site?");
        if (!confirmed)
            return false;
    });


    $("#add-binding").click(function () {
        var bindings = $("#bindings");
        var newBinding = bindings.find("tr:last").clone();
        newBinding.find("td input").val("");
        bindings.append(newBinding);
        bindings.find("tr td img").removeClass("hide");
        newBinding.find("td input").focus();

    });

    $("#bindings").on("click", "tr td img.remove-binding", function () {
        if ($("#bindings tr").length > 1) {
            $(this).parent().parent().remove();
        }

        if ($("#bindings tr").length == 1) {
            $("#bindings tr td img").addClass("hide");
        }

    });
    
    $(window).resize(function () {
        $("#menu").height($(document).height());
    });

    $(function() {
        $("#menu").height($(document).height());
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

                var group = input.parents(".control-group");
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
