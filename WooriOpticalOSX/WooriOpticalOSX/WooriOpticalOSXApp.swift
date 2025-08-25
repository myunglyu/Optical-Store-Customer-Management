//
//  WooriOpticalOSXApp.swift
//  WooriOpticalOSX
//
//  Created by Myung Lyu on 8/25/25.
//

import SwiftUI

@main
struct WooriOpticalOSXApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
        .commands {
            CommandGroup(replacing: .newItem) { }
        }
    }
}
