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
});
