#include "pch.h"
#include "Direct3DBase.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::UI::Core;
using namespace Windows::Foundation;
using namespace Windows::Graphics::Display;

// コンストラクター。
Direct3DBase::Direct3DBase()
{
}

// 実行するのに必要な Direct3D リソースを初期化します。
void Direct3DBase::Initialize()
{
	CreateDeviceResources();
}

// これらは、デバイスに依存するリソースです。
void Direct3DBase::CreateDeviceResources()
{
	// このフラグは、カラー チャネルの順序が API の既定値とは異なるサーフェスのサポートを追加します。 
	// これは、Direct2D との互換性を保持するために必要です。
	UINT creationFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;

#if defined(_DEBUG)
	// プロジェクトがデバッグ ビルドに含まれる場合、このフラグを使用して SDK レイヤーによるデバッグを有効にします。
	creationFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

	// この配列では、このアプリケーションでサポートされる DirectX ハードウェア機能レベルのセットを定義します。
	// 順序が保存されることに注意してください。
	// アプリケーションの最低限必要な機能レベルをその説明で宣言することを忘れないでください。
	//特に記載がない限り、  すべてのアプリケーションは 9.3 をサポートすることが想定されます。
	D3D_FEATURE_LEVEL featureLevels[] = 
	{
		D3D_FEATURE_LEVEL_9_3
	};

	// Direct3D 11 API デバイス オブジェクトと、対応するコンテキストを作成します。
	ComPtr<ID3D11Device> device;
	ComPtr<ID3D11DeviceContext> context;
	DX::ThrowIfFailed(
		D3D11CreateDevice(
			nullptr, // 既定のアダプターを使用する nullptr を指定します。
			D3D_DRIVER_TYPE_HARDWARE,
			nullptr,
			creationFlags, // デバッグ フラグと Direct2D 互換性フラグを設定します。
			featureLevels, // このアプリがサポートできる機能レベルの一覧を表示します。
			ARRAYSIZE(featureLevels),
			D3D11_SDK_VERSION, // これには常に D3D11_SDK_VERSION を設定します。
			&device, // 作成された Direct3D デバイスを返します。
			&m_featureLevel, // 作成されたデバイスの機能レベルを返します。
			&context // デバイスのイミディエイト コンテキストを返します。
			)
		);

	// Direct3D 11.1 API のデバイス インターフェイスとコンテキスト インターフェイスを取得します。
	DX::ThrowIfFailed(
		device.As(&m_d3dDevice)
		);

	DX::ThrowIfFailed(
		context.As(&m_d3dContext)
		);
}

// ウィンドウ サイズに依存するすべてのメモリ リソースを割り当てます。
void Direct3DBase::CreateWindowSizeDependentResources()
{
	// レンダー ターゲットのバッファー用の記述子を作ります。
	CD3D11_TEXTURE2D_DESC renderTargetDesc(
		DXGI_FORMAT_B8G8R8A8_UNORM,
		static_cast<UINT>(m_renderTargetSize.Width),
		static_cast<UINT>(m_renderTargetSize.Height),
		1,
		1,
		D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE
		);
	renderTargetDesc.MiscFlags = D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX | D3D11_RESOURCE_MISC_SHARED_NTHANDLE;

	// レンダー ターゲットのバッファーとして 2-D サーフェイスを割り当てます。
	DX::ThrowIfFailed(
		m_d3dDevice->CreateTexture2D(
			&renderTargetDesc,
			nullptr,
			&m_renderTarget
			)
		);

	DX::ThrowIfFailed(
		m_d3dDevice->CreateRenderTargetView(
			m_renderTarget.Get(),
			nullptr,
			&m_renderTargetView
			)
		);

	// 深度ステンシル ビューを作成します。
	CD3D11_TEXTURE2D_DESC depthStencilDesc(
		DXGI_FORMAT_D24_UNORM_S8_UINT,
		static_cast<UINT>(m_renderTargetSize.Width),
		static_cast<UINT>(m_renderTargetSize.Height),
		1,
		1,
		D3D11_BIND_DEPTH_STENCIL
		);

	ComPtr<ID3D11Texture2D> depthStencil;
	DX::ThrowIfFailed(
		m_d3dDevice->CreateTexture2D(
			&depthStencilDesc,
			nullptr,
			&depthStencil
			)
		);

	CD3D11_DEPTH_STENCIL_VIEW_DESC depthStencilViewDesc(D3D11_DSV_DIMENSION_TEXTURE2D);
	DX::ThrowIfFailed(
		m_d3dDevice->CreateDepthStencilView(
			depthStencil.Get(),
			&depthStencilViewDesc,
			&m_depthStencilView
			)
		);

	// ウィンドウ全体をターゲットとするレンダリング ビューポートを作成します。
	CD3D11_VIEWPORT viewport(
		0.0f,
		0.0f,
		m_renderTargetSize.Width,
		m_renderTargetSize.Height
		);

	m_d3dContext->RSSetViewports(1, &viewport);
}

void Direct3DBase::UpdateForRenderResolutionChange(float width, float height)
{
	m_renderTargetSize.Width = width;
	m_renderTargetSize.Height = height;

	ID3D11RenderTargetView* nullViews[] = {nullptr};
	m_d3dContext->OMSetRenderTargets(ARRAYSIZE(nullViews), nullViews, nullptr);
	m_renderTarget = nullptr;
	m_renderTargetView = nullptr;
	m_depthStencilView = nullptr;
	m_d3dContext->Flush();
	CreateWindowSizeDependentResources();
}

void Direct3DBase::UpdateForWindowSizeChange(float width, float height)
{
	m_windowBounds.Width  = width;
	m_windowBounds.Height = height;
}
