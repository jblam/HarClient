export interface ITestCase {
    readonly name: string;
    run: () => Promise<any>;
}

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
]

function fetchTestCase(name: string, input: RequestInfo, init?: RequestInit): ITestCase {
    return {
        name,
        run: () => fetch(input, init)
    };
}
