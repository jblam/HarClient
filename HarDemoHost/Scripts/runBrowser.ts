import * as tests from "./testcase.js"

if (!addButtonListener()) {
    document.addEventListener('DOMContentLoaded', _ => {
        if (!addButtonListener()) {
            alert("Borked");
        }
    })
}

function addButtonListener(): boolean {
    let button = document.getElementById("run");
    let statusTable = <HTMLTableElement>document.getElementById("status");
    if (button && statusTable) {
        button.addEventListener('click', _ => asdf(tests.arr, statusTable));
        document.body.classList.remove("loading");
    }
    return !!button;

    async function asdf(cases: ReadonlyArray<tests.ITestCase>, status: HTMLTableElement) {
        var tbody = document.createElement("tbody");
        status.appendChild(tbody);
        var results: tests.ITestCaseResults = {};
        for (const c of cases) {
            var tr = document.createElement("tr");
            var nameCell = document.createElement("td");
            var statusCell = document.createElement("td");
            tr.appendChild(nameCell);
            tr.appendChild(statusCell);
            tbody.appendChild(tr);
            nameCell.textContent = c.name;
            statusCell.textContent = "Running";
            try {
                var thisResult = c.run(results);
                results[c.name] = thisResult;
                await thisResult;
                statusCell.textContent = "Complete";
            } catch (err) {
                statusCell.textContent = (err || "Failed").toString();
            }
        }
    }
}