export interface ITestCase {
    readonly name: string;
    run: (results: ITestCaseResults) => Promise<any>;
}
export interface ITestCaseResults {
    [k: string]: Promise<any>;
}
document.cookie = "key=value;path=/api;expires=Thu, 01-Jan-1970 00:00:01 GMT";

export const arr: ReadonlyArray<ITestCase> = [
    fetchTestCase("OK", "/api/behaviour/content"),
    fetchTestCase("Redirect", "/api/behaviour/redirect"),
    fetchTestCase("Get query string", "/api/behaviour/content?q1=a1&q2=a2"),
    fetchTestCase("Post JSON", "/api/behaviour/accepted", {
        method: 'POST',
        body: JSON.stringify({ content: 'yes' }),
        headers: { 'Content-Type': 'application/json' }
    }),
    fetchTestCase("Accepted with redirect", "/api/behaviour/accepted?location=/api/behaviour/content", { method: 'POST' }),
    fetchTestCase("Get XML", "/api/behaviour/content", { headers: { Accept: 'application/xml' } }),
    {
        name: "GET binary content",
        run: async () => await (await fetch("/chemistry-dog.jpg")).blob()
    },
    {
        name: "POST binary content",
        run: async (results) => {
            let blob = await results["GET binary content"];
            await fetch("/api/behaviour/accepted", {
                method: 'POST',
                headers: { "Content-Type": "image/jpg" },
                body: blob
            });
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
            await fetch("/api/behaviour/accepted", {
                method: 'POST',
                body: formData
            });
        }
    },
    fetchTestCase("POST multipart form", "/api/behaviour/accepted", {
        method: 'POST',
        body: (() => {
            // produces application/form-multipart
            let f = new FormData();
            f.append("x", "x");
            f.append("y", "a+b+c/d:e?f");
            return f;
        })()
    }),
    fetchTestCase("POST urlencoded", "/api/behaviour/accepted", {
        method: 'POST',
        body: (() => {
            // produces application/x-www-form-urlencoded
            // equivalent to an actual genuine <form>
            let p = new URLSearchParams();
            p.append("x", "x");
            p.append("y", "a+b+c/d:e?f");
            return p;
        })()
    }),
    {
        name: "cancelled request",
        run: async () => {
            let abort = new AbortController();
            let fetchResult = fetch("/api/behaviour/delay?delay_ms=5000",
                { signal: abort.signal });
            setTimeout(() => abort.abort(), 100);
            return fetchResult;
        }
    },
    {
        name: "POST set cookies",
        run: async () => {
            var response = await fetch("/api/behaviour/set-cookie", { method: 'POST' });
            if (!document.cookie) {
                throw new Error("`document.cookie` is still empty");
            }
        }
    },
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

function fetchTestCase(name: string, input: RequestInfo, init?: RequestInit): ITestCase {
    return {
        name,
        run: () => fetch(input, init)
    };
}
