// src/app/page.tsx
"use client";

import { useState } from "react";

export default function Home() {
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [jobRequirements, setJobRequirements] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!cvFile) { setError("Please select a CV file."); return; }
    if (!jobRequirements.trim()) { setError("Please enter job requirements."); return; }

    setLoading(true);
    setError(null);
    setResult(null);

    const formData = new FormData();
    formData.append("cvFile", cvFile);
    formData.append("jobRequirements", jobRequirements);

    try {
      const response = await fetch("http://localhost:5068/api/analyses", {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const err = await response.json();
        setError(JSON.stringify(err, null, 2));
        return;
      }

      const data = await response.json();
      setResult(data);
    } catch (err) {
      setError("Failed to connect to backend. Make sure it is running on port 5068.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-gray-50 flex items-center justify-center p-8">
      <div className="bg-white rounded-2xl shadow-md w-full max-w-2xl p-8">
        <h1 className="text-2xl font-bold text-gray-800 mb-2">CV Pipeline</h1>
        <p className="text-gray-500 text-sm mb-8">Upload a candidate CV and paste the job requirements to generate a tailored document.</p>

        <form onSubmit={handleSubmit} className="space-y-6">

          {/* CV File Upload */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Candidate CV (.docx or .txt)
            </label>
            <input
              type="file"
              accept=".docx,.txt"
              onChange={(e) => setCvFile(e.target.files?.[0] ?? null)}
              className="block w-full text-sm text-gray-500
                file:mr-4 file:py-2 file:px-4
                file:rounded-lg file:border-0
                file:text-sm file:font-medium
                file:bg-blue-50 file:text-blue-700
                hover:file:bg-blue-100"
            />
            {cvFile && (
              <p className="mt-1 text-xs text-gray-400">Selected: {cvFile.name}</p>
            )}
          </div>

          {/* Job Requirements */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Job Requirements (RFQ text)
            </label>
            <textarea
              value={jobRequirements}
              onChange={(e) => setJobRequirements(e.target.value)}
              rows={10}
              placeholder="Paste the full job requirements / RFQ text here..."
              className="w-full rounded-lg border border-gray-300 p-3 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>

          {/* Error */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <p className="text-sm text-red-700 whitespace-pre-wrap">{error}</p>
            </div>
          )}

          {/* Submit */}
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300
              text-white font-medium py-3 px-6 rounded-lg transition-colors"
          >
            {loading ? "Processing..." : "Generate CV"}
          </button>
        </form>

        {/* Result */}
        {result && (
          <div className="mt-8">
            <h2 className="text-lg font-semibold text-gray-700 mb-3">Result</h2>
            <div className="bg-gray-50 rounded-lg border border-gray-200 p-4 overflow-auto max-h-96">
              <pre className="text-xs text-gray-600 whitespace-pre-wrap">
                {JSON.stringify(result, null, 2)}
              </pre>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}