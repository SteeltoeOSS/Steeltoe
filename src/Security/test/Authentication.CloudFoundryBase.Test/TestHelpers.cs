// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public static class TestHelpers
{
    public static string GetValidTokenRequestResponse()
    {
        return
            @"{""access_token"":""eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiJjNjUxZGI2NjllODM0NGNkOTMwMGUxYjJkYjUxOTgwZCIsInN1YiI6IjEzYmI2ODQxLWU0ZDYtNGE5YS04NzZjLTllZjEzYWE2MWNjNyIsInNjb3BlIjpbIm9wZW5pZCJdLCJjbGllbnRfaWQiOiI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJjaWQiOiI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJhenAiOiI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJncmFudF90eXBlIjoiYXV0aG9yaXphdGlvbl9jb2RlIiwidXNlcl9pZCI6IjEzYmI2ODQxLWU0ZDYtNGE5YS04NzZjLTllZjEzYWE2MWNjNyIsIm9yaWdpbiI6InVhYSIsInVzZXJfbmFtZSI6InRlc3Rzc291c2VyIiwiZW1haWwiOiJ0ZXN0c3NvdXNlckB0ZXN0Y2xvdWQuY29tIiwiYXV0aF90aW1lIjoxNDcyNzUxNDUyLCJyZXZfc2lnIjoiNTExMzU4NDIiLCJpYXQiOjE0NzI3NTE0NTIsImV4cCI6MTQ3Mjc5NDY1MiwiaXNzIjoiaHR0cHM6Ly90ZXN0c3NvLnVhYS5zeXN0ZW0udGVzdGNsb3VkLmNvbS9vYXV0aC90b2tlbiIsInppZCI6ImVmMzA0ZWQwLTNhNWEtNDQyZS05YWQ4LWU0Y2EzYWQ1MTIwZSIsImF1ZCI6WyI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJvcGVuaWQiXX0.f6CA7WrGrY__CNh - RvY1pl05 - 2PQjeyE7C_OH48ChfoxsL4hO8BJEofz934_Jox6dXJwp3wyeMPYwyS5fKGA3ASJT5KqdZ8QdHhOj7lbcejwTUUfG_t - K26W39BLT - cEbpTFyHzBUK79fhIfwSmmXJWU0vQaZ3F0dYKRm98rCEQo9s5jr2jrqHep52vL3Xo8mwDJO4GBc85p8zzVpUPOYMkKiWfEujIOvBL0yTqAL64NsYmFupbKyt5dgf - eF - 1GOuP7Rbgbk_llQuFOkwYr4dLzgw045wXeU3uzJzTrGNzIlJQPOijU1w2uBgYi7fT2wgJDKb4toxot2_6wSXpqBw"",""token_type"":""bearer"",""refresh_token"":""eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiJjNjUxZGI2NjllODM0NGNkOTMwMGUxYjJkYjUxOTgwZC1yIiwic3ViIjoiMTNiYjY4NDEtZTRkNi00YTlhLTg3NmMtOWVmMTNhYTYxY2M3Iiwic2NvcGUiOlsib3BlbmlkIl0sImlhdCI6MTQ3Mjc1MTQ1MiwiZXhwIjoxNDc1MzQzNDUyLCJjaWQiOiI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJjbGllbnRfaWQiOiI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJpc3MiOiJodHRwczovL3Rlc3Rzc28udWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoiZWYzMDRlZDAtM2E1YS00NDJlLTlhZDgtZTRjYTNhZDUxMjBlIiwiZ3JhbnRfdHlwZSI6ImF1dGhvcml6YXRpb25fY29kZSIsInVzZXJfbmFtZSI6InRlc3Rzc291c2VyIiwib3JpZ2luIjoidWFhIiwidXNlcl9pZCI6IjEzYmI2ODQxLWU0ZDYtNGE5YS04NzZjLTllZjEzYWE2MWNjNyIsInJldl9zaWciOiI1MTEzNTg0MiIsImF1ZCI6WyI0NGY4NTRmZC00OWZlLTQxMDItYjBkMS1hNTI2ZGJmZjkzZDkiLCJvcGVuaWQiXX0.WMEZtwvoJXU8njysozaNglmpYouxoY65AirXC3ypdLsnSVE_OtePQQi3hb3YPQDbrc6JAETnCGcDBJVNUPsnlg4g_O_RPLejB4ZlMioRTZRNv9gzOlELqjgBLAVEkWOiBFuH7hwbR7X7DlmFTcDFSc - gQY5TaLn - wR6PVtQM3oRw_nx1f3 - xgDGcYx7Ktk9rov - xF_C0Pzsoet4dvG78YUlnXgR08KwJnDX_ElV - 0vTOtybqjs_XGSS9N39EWeeykCzciddhqVCfmMM5VMCVaP - 7pr - VDz0N8ArSWfOobI4nPqcj50wvPb595SgId - NlNP4fBX1ibnCeOxWwuzId0w"",""expires_in"":43199,""scope"":""openid"",""jti"":""c651db669e8344cd9300e1b2db51980d""}";
    }

    public static string GetValidTokenInfoRequestResponse()
    {
        return
            @"{""user_id"":""13bb6841-e4d6-4a9a-876c-9ef13aa61cc7"",""user_name"":""testssouser"",""email"":""testssouser@testcloud.com"",""client_id"":""44f854fd-49fe-4102-b0d1-a526dbff93d9"",""exp"":1472803463,""scope"":[""openid""],""jti"":""cc956120c52a431fbbb8945ac42872fd"",""aud"":[""44f854fd-49fe-4102-b0d1-a526dbff93d9"",""openid""],""sub"":""13bb6841-e4d6-4a9a-876c-9ef13aa61cc7"",""iss"":""https://testsso.uaa.system.testcloud.com/oauth/token"",""iat"":1472760263,""cid"":""44f854fd-49fe-4102-b0d1-a526dbff93d9"",""grant_type"":""authorization_code"",""azp"":""44f854fd-49fe-4102-b0d1-a526dbff93d9"",""auth_time"":1472760254,""zid"":""ef304ed0-3a5a-442e-9ad8-e4ca3ad5120e"",""rev_sig"":""51135842"",""origin"":""uaa"",""revocable"":false}";
    }
}
