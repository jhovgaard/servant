$(function () {
    var $searchTextbox = $("#search input[type=text]");
    var searchResultInFocus = null;
    $searchTextbox.watermark("Search");

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
            var $certificate = $(this).parents('tbody').find('tr.certificate');
            console.log($certificate)
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
        newBinding.find("td input").val("");
        newBinding.find("td").removeClass("error");
        newBinding.find("td input[name=bindingsipaddress]").val('*');
        newBinding.find("td span.help-block, td span.help-inline").remove();
        bindings.append(newBinding);
        bindings.find("tr td img").removeClass("hide");
        newBinding.find("td input").first().focus();
        return false;
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
    
    // Activate search by press "/"
    $("body").keypress(function (e) {
        var activeElement = $(document.activeElement);
        if (e.which == 47 && !activeElement.is(':input')) {
            var charcheck = /[a-zA-Z0-9]/;
            var keyPressed = String.fromCharCode(e.which);
            if (!charcheck.test(keyPressed)) {
                keyPressed = '';
            }
            var query = $searchTextbox.val() + keyPressed;
            $searchTextbox.focus().val(query);
            return false;
        }
    });
    
    // Search
    $searchTextbox.keyup(function (e) {
        var $sidemenu = $('#sidemenu');
        var searchresults = $('#searchresults');

        if (e.keyCode == 27) { //esc
            $searchTextbox.val('');
            $sidemenu.removeClass('search');
            return false;
        }
        if (e.keyCode == 13) {
            var link = searchresults.find('a:visible.active');
            if (!link.length)
                link = searchresults.find('a:visible:first');

            if (link.length) {
                window.location = link.attr("href");
            }
            return false;
        };


        if ((e.keyCode == 40 || e.keyCode == 38) && searchresults.length) { //40 == arrow down, 38 == arrow up
            var results = searchresults.find('a:visible');
            if (searchResultInFocus == null) {
                var item;
                if (e.keyCode == 38) {
                    item = results.last();
                    searchResultInFocus = results.length;
                } else {
                    item = results.first();
                    searchResultInFocus = 0;
                }
                item.addClass('active');
            } else {
                var newIndex;
                if (e.keyCode == 38) { // key up
                    newIndex = (searchResultInFocus - 1);
                    if (newIndex < 0)
                        newIndex = results.length - 1;
                } else {
                    newIndex = (searchResultInFocus + 1);
                    if (newIndex > results.length - 1)
                        newIndex = 0;
                }

                var newItem = $(results[newIndex]);
                if (newItem.length) {
                    results.removeClass('active');
                    newItem.addClass('active');
                    searchResultInFocus = newIndex;
                }
            }
        }

        var val = $searchTextbox.val();
        if (val.length > 0) {
            if (!$sidemenu.hasClass('search')) {
                $sidemenu.addClass('search');

                if (!searchresults.length) {
                    searchresults = $('<div id="searchresults"/>');
                    $sidemenu.find("a").each(function (i, item) {
                        var $item = $(item);
                        var result = $('<a href="' + $item.attr('href') + '" data-filtr="' + $item.text() + '">' + $item.text() + '</a>');
                        searchresults.append(result);
                    });
                    $('#sidemenu').append(searchresults);

                    $searchTextbox.filtr(searchresults.find('a'), {
                        wait: 0
                    });
                }
            }
        } else {
            $sidemenu.removeClass('search');
        }
    });

    // Hide popup
    $("#popup-message").click(function () {
        $(this).slideUp();
    });

    // Toggle submenu
    $("#sidemenu > ul > li > a").click(function () {
        var $a = $(this).parent().find('a:first');
        var $selectedSubUl = $(this).parent().find("ul");

        if ($selectedSubUl.length) {
            var visible = $selectedSubUl.is(':visible');
            var $parentUrl = $a.parents('ul');
            var $li = $a.parent();
            if (!visible) {
                $li.addClass('active');
                $selectedSubUl.slideDown('fast');
            } else {
                $li.removeClass('active');
                $selectedSubUl.slideUp('fast');
            }
        }
    });

});


