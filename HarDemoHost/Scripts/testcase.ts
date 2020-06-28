export interface ITestCase {
    readonly name: string;
    run: (results: ITestCaseResults) => Promise<any>;
}
export interface ITestCaseResults {
    [k: string]: Promise<any>;
}

export const arr: ReadonlyArray<ITestCase> = [
    fetchTestCase("OK", { input: "/api/behaviour/content" }),
    fetchTestCase("Redirect", { input: "/api/behaviour/redirect" }),
    fetchTestCase("Get query string", { input: "/api/behaviour/content?q1=a1&q2=a2" }),
    fetchTestCase("Post JSON", {
        input: "/api/behaviour/accepted",
        init: {
            method: 'POST',
            body: JSON.stringify({ content: 'yes' }),
            headers: { 'Content-Type': 'application/json' }
        }
    }),
    fetchTestCase("Not found", { input: "/api/behaviour/not-found" }, r => {
        if (r.status != 404)
            throw new Error("Expected HTTP status 404, but received " + r.status);
    }),
    fetchTestCase("Accepted with redirect", { input: "/api/behaviour/accepted?location=/api/behaviour/content", init: { method: 'POST' } }),
    fetchTestCase("Get XML",
        { input: "/api/behaviour/content", init: { headers: { Accept: 'application/xml' } } },
        response => {
            assertIsSuccess(response);
            if (!response.headers.get("content-type").startsWith("application/xml")) {
                throw new Error("Server did not provide XML; probably reverted to JSON");
            }
        }
    ),
    {
        name: "GET binary content",
        run: async () => {
            let response = await fetch("/chemistry-dog.jpg");
            assertIsSuccess(response);
            return await (response).blob();
        }
    },
    {
        name: "POST binary content",
        run: async (results) => {
            let blob = await results["GET binary content"];
            let response = await fetch("/api/behaviour/accepted", {
                method: 'POST',
                headers: { "Content-Type": "image/jpg" },
                body: blob
            });
            assertIsSuccess(response);
        }
    },
    {
        name: "POST multipart form with binary",
        run: async (results) => {
            // get the blob out of the previous test result,
            // append it to the form, producing application/form-multipart
            let blob = await results["GET binary content"];
            let formData = new FormData();
            formData.append("textData", "text");
            formData.append("imageData", blob, "image.jpg");
            let response = await fetch("/api/behaviour/accepted", {
                method: 'POST',
                body: formData
            });
            assertIsSuccess(response);
        }
    },
    fetchTestCase("POST multipart form", {
        input: "/api/behaviour/accepted",
        init: {
            method: 'POST',
            body: (() => {
                // produces application/form-multipart
                let f = new FormData();
                f.append("x", "x");
                f.append("y", "a+b+c/d:e?f");
                return f;
            })()
        }
    }),
    fetchTestCase("POST urlencoded", {
        input: "/api/behaviour/accepted",
        init: {
            method: 'POST',
            body: (() => {
                // produces application/x-www-form-urlencoded
                // equivalent to an actual genuine <form>
                let p = new URLSearchParams();
                p.append("x", "x");
                p.append("y", "a+b+c/d:e?f");
                return p;
            })()
        }
    }),
    {
        name: "cancelled request",
        run: async () => {
            let abort = new AbortController();
            let fetchResult = fetch("/api/behaviour/delay?delay_ms=5000",
                { signal: abort.signal });
            setTimeout(() => abort.abort(), 100);
            try {
                await fetchResult;
                throw new Error("The aborted request unexpectedly completed");
            } catch (err) {
                if (err instanceof DOMException && err.name == "AbortError") {
                    return err;
                } else {
                    throw new Error(`Unexpected error: ${err.toString()}`);
                }
            }
        }
    },
    fetchTestCase("POST set cookies", { input: "/api/behaviour/set-cookie", init: { method: 'POST' } }),
    {
        name: "GET check cookies",
        run: async () => {
            var response = await fetch("/api/behaviour/check-cookie", {
                credentials: "same-origin"
            });
            if (response.status > 299) {
                throw new Error("Cookie check failed");
            }
        }
    }
];

function assertIsSuccess(response: Response) {
    if (response.status < 200 || response.status > 299)
        throw new Error("Response status does not indicate success");
    return response;
}
function fetchTestCase(name: string, fetchArgs: { input: RequestInfo, init?: RequestInit }, validation?: (response: Response) => any): ITestCase {
    let { input, init } = fetchArgs;
    return {
        name,
        run: () => fetch(input, init).then(validation || assertIsSuccess)
    };
}

