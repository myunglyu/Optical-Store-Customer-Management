//
//  ContentView.swift
//  WooriOpticalOSX
//
//  Created by Myung Lyu on 8/25/25.
//

import SwiftUI
import WebKit

struct ContentView: View {
    @State private var isLoading = true
    @State private var canGoBack = false
    @State private var canGoForward = false
    @State private var isServerReady = false
    @State private var currentURL = "http://localhost:5000"
    
    var body: some View {
        VStack {
            // Main content
            if !isServerReady {
                VStack(spacing: 20) {
                    ProgressView("Connecting to Woori Optical...")
                        .progressViewStyle(CircularProgressViewStyle())
                    
                    Text("Make sure your .NET server is running on:")
                        .font(.caption)
                        .foregroundColor(.gray)
                    
                    Text(currentURL)
                        .font(.caption)
                        .foregroundColor(.blue)
                        .textSelection(.enabled)
                    
                    Button("Try Connect") {
                        checkServerAndConnect()
                    }
                    .buttonStyle(.borderedProminent)
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                SimpleWebView(
                    url: URL(string: currentURL)!,
                    isLoading: $isLoading,
                    canGoBack: $canGoBack,
                    canGoForward: $canGoForward
                )
            }
        }
        .onAppear {
            checkServerAndConnect()
        }
    }
    
    private func checkServerAndConnect() {
        let ports = ["5002", "5001", "5000"]
        var currentIndex = 0
        
        func tryNextPort() {
            guard currentIndex < ports.count else {
                DispatchQueue.main.async {
                    isServerReady = false
                }
                return
            }
            
            let port = ports[currentIndex]
            let testURL = "http://localhost:\(port)"
            currentIndex += 1
            
            // Simple ping test
            guard let url = URL(string: testURL) else {
                tryNextPort()
                return
            }
            
            var request = URLRequest(url: url)
            request.timeoutInterval = 3.0
            request.cachePolicy = .reloadIgnoringLocalAndRemoteCacheData
            
            URLSession.shared.dataTask(with: request) { data, response, error in
                DispatchQueue.main.async {
                    if let httpResponse = response as? HTTPURLResponse,
                       httpResponse.statusCode == 200 {
                        currentURL = testURL
                        isServerReady = true
                        print("Connected to server at \(testURL)")
                    } else {
                        tryNextPort()
                    }
                }
            }.resume()
        }
        
        tryNextPort()
    }
}

// Simple macOS WebView wrapper
struct SimpleWebView: NSViewRepresentable {
    let url: URL
    @Binding var isLoading: Bool
    @Binding var canGoBack: Bool
    @Binding var canGoForward: Bool
    
    func makeNSView(context: Context) -> WKWebView {
        let config = WKWebViewConfiguration()
        
        // Use modern API instead of deprecated javaScriptEnabled
        if #available(macOS 11.0, *) {
            // JavaScript is enabled by default in modern WKWebView
            // We can control it per-navigation if needed in the delegate
        } else {
            // Fallback for older versions
            config.preferences.javaScriptEnabled = true
        }
        
        let webView = WKWebView(frame: .zero, configuration: config)
        webView.navigationDelegate = context.coordinator
        webView.allowsBackForwardNavigationGestures = true
        
        return webView
    }
    
    func updateNSView(_ webView: WKWebView, context: Context) {
        let request = URLRequest(url: url)
        webView.load(request)
    }
    
    func makeCoordinator() -> Coordinator {
        Coordinator(self)
    }
    
    class Coordinator: NSObject, WKNavigationDelegate {
        let parent: SimpleWebView
        
        init(_ parent: SimpleWebView) {
            self.parent = parent
        }
        
        func webView(_ webView: WKWebView, didStartProvisionalNavigation navigation: WKNavigation!) {
            DispatchQueue.main.async {
                self.parent.isLoading = true
            }
        }
        
        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            DispatchQueue.main.async {
                self.parent.isLoading = false
                self.parent.canGoBack = webView.canGoBack
                self.parent.canGoForward = webView.canGoForward
            }
        }
        
        func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) {
            DispatchQueue.main.async {
                self.parent.isLoading = false
            }
            print("WebView navigation failed: \(error.localizedDescription)")
        }
        
        // Optional: Control JavaScript on a per-navigation basis if needed
        @available(macOS 11.0, *)
        func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction, preferences: WKWebpagePreferences, decisionHandler: @escaping (WKNavigationActionPolicy, WKWebpagePreferences) -> Void) {
            
            // Enable JavaScript for all navigations (this is the default behavior)
            preferences.allowsContentJavaScript = true
            
            decisionHandler(.allow, preferences)
        }
    }
}

#Preview {
    ContentView()
}
