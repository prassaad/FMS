    $(document).ready(function () {
        $("ul#tabs li").click(function (e) {
            if (!$(this).hasClass("active-step")) {
                    var tabNum = $(this).index();
                    var nthChild = tabNum + 1;
                    tabMoves(nthChild);
            }
        });

        $(".next-tab").click(function () {

            var id = $(this).attr("id");
            if (id != "finish") {
                var tabNum = id.split('_')[1];
                var nthChild = parseInt(tabNum) + 1;
                tabMoves(nthChild);
            }
            else {
                alert("tabs completed..");
            }
        });

        $(".prev-tab").click(function () {
            var id = $(this).attr("id");
            var tabNum = id.split('_')[1];
            var nthChild = parseInt(tabNum) - 1;
            tabMoves(nthChild);
        });

        function tabMoves(nthChild) {
            $("ul#tabs li.active-step").removeClass("active-step");
            $("#tab-hd-" + nthChild).addClass("active-step");
            $("ul#tab li.current-tab").removeClass("current-tab");
            $("ul#tab li:nth-child(" + nthChild + ")").addClass("current-tab");
        }
    });